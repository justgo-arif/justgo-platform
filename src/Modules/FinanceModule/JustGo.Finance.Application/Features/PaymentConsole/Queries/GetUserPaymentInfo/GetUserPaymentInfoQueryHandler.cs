using Adyen.Model.Checkout;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetSavedCardDetails;
using Pipelines.Sockets.Unofficial.Buffers;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetUserPaymentInfo
{
    public class GetUserPaymentInfoQueryHandler : IRequestHandler<GetUserPaymentInfoQuery, List<UserPaymentInfoDto>>
    {
        private readonly LazyService<IReadRepository<InstallmentDto>> _readRepository;
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IMediator _mediator;

        public GetUserPaymentInfoQueryHandler(LazyService<IReadRepository<InstallmentDto>> readRepository, ISystemSettingsService systemSettingsService, IMediator mediator)
        {
            _readRepository = readRepository;
            _systemSettingsService = systemSettingsService;
            _mediator = mediator;
        }

        public async Task<List<UserPaymentInfoDto>> Handle(GetUserPaymentInfoQuery request, CancellationToken cancellationToken)
        {
            var hostsystemid = await _systemSettingsService.GetSystemSettingsByItemKey(
               "CLUBPLUS.HOSTSYSTEMID", cancellationToken);
            var enableAdyenPayment = await _systemSettingsService.GetSystemSettingsByItemKey(
                "SYSTEM.PAYMENT.EnableAdyenPayment", cancellationToken);
            bool isAdyenEnabled = !string.Equals(enableAdyenPayment ?? "false", "false", StringComparison.OrdinalIgnoreCase);
            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.OwnerId), cancellationToken);

            var (sql, queryParams) = BuildSqlAndParams(request.UserIds, ownerId);

            await using var result = await _readRepository.Value.GetMultipleQueryAsync(sql, cancellationToken, queryParams, null, "text");
            var users = (await result.ReadAsync<UserPaymentInfoDto>()).ToList();
            var invoicedetails = (await result.ReadAsync<InvoiceDetails>()).ToList();
            var storedCards = (await result.ReadAsync<StoredCard>()).ToList();
            var storedStripeCards = (await result.ReadAsync<RecurringPaymentCardInfo>()).ToList();

            await PopulateUserCardsAsync(users, storedCards, invoicedetails, storedStripeCards, isAdyenEnabled, hostsystemid, cancellationToken);

            return users;
        }
        private static (string sql, DynamicParameters queryParams) BuildSqlAndParams(List<Guid> userIds,int ownerId)
        {
            var queryParams = new DynamicParameters();
            queryParams.Add("UserSyncId", userIds);
            queryParams.Add("OwnerId", ownerId);

            string commonJoin = string.Empty;
            commonJoin += $@"
                    INNER JOIN ProcessInfo p ON ou.MemberDocId = p.PrimaryDocId
                    INNER JOIN [State] S ON S.StateId = p.CurrentStateId  AND S.[Name]<>'Merged'
                ";
            string commoncondition = string.Empty;

            var sql = $@"
                    select ou.MemberDocId as DocId,
                         ou.UserId, 
                        CAST(ou.UserSyncId AS nvarchar(255)) as UserSyncId,  
                        ISNULL(ou.FirstName,'') as FirstName,
                        ISNULL(ou.LastName,'') as LastName,
                        ISNULL(ou.ProfilePicURL,'') as Image,
                        ou.MemberId AS MID ,  
                        ou.MemberDocId,
                        ou.EmailAddress 
                    from [User] ou 
                    
                    {commonJoin}
                    where ou.UserSyncId IN @UserSyncId 
                    {commoncondition}

                    select  
                        ou.UserId, 
                        CAST(ou.UserSyncId AS nvarchar(255)) as UserSyncId, 
                        CONCAT(ou.FirstName, ' ', ou.LastName) AS InvoiceTo,
                        ou.EmailAddress,
                        ISNULL(ou.Country,'') as Country,
                        ISNULL(ou.CountryId,0) as CountryId,
                        ISNULL(ou.Address1,'') as Address1,
                        ISNULL(ou.Address2,'') as Address2,
                        ISNULL(ou.Address3,'') as Address3,
                        ISNULL(ou.Town,'') as Town,
                        ISNULL(ou.County,'') as County,
                        ISNULL(ou.CountyId,0) as CountyId,
                        ISNULL(ou.PostCode,'') as PostCode,
                        '' as PoNumber,
                        '' as TaxId,
                        '' as Note
                    from [User] ou 
                    {commonJoin}
                    where ou.UserSyncId IN @UserSyncId 
                    {commoncondition}

                     SELECT DISTINCT 
                        ou.UserId,
                        CAST(ou.UserSyncId AS nvarchar(255)) AS UserSyncId,
                        rc.Id AS RecurringPaymentCustomerId,
                        rc.Tag,
                        rc.Metadata
                     FROM RecurringPaymentCustomer rc
                    INNER JOIN [User] ou 
                        ON rc.OwnerUserId = ou.UserId
                       INNER JOIN recurringpaymentplan rpp on   rpp.customerid = rc.id
                          INNER JOIN Products_Default pd 
                       ON pd.DocId = rpp.ProductId
                    WHERE ou.UserSyncId IN @UserSyncId
                      AND rc.paymentmethod = 'Card'
                      AND ISJSON(rc.Tag) = 0
                      AND rc.Tag <> ''
                      AND ISNULL(pd.OwnerId,0) =@OwnerId 
                    UNION

                    SELECT DISTINCT 
                        ou.UserId,
                        CAST(ou.UserSyncId AS nvarchar(255)) AS UserSyncId,
                        rc.Id AS RecurringPaymentCustomerId,
                        rc.Tag,
                        rc.Metadata
                    FROM RecurringPaymentCustomer rc
                    INNER JOIN RecurringPaymentPlan rpp 
                        ON rc.Id = rpp.CustomerId
                         INNER JOIN Products_Default pd 
                       ON pd.DocId = rpp.ProductId
                    INNER JOIN [User] ou 
                        ON rpp.ForEntityId = ou.MemberDocId   
                    WHERE ou.UserSyncId IN @UserSyncId
                      AND rc.paymentmethod = 'Card'
                      AND ISJSON(rc.Tag) = 0
                      AND rc.Tag <> ''
                      AND rc.OwnerUserId <> ou.UserId
                      AND ISNULL(pd.OwnerId,0) =@OwnerId 


                    Select distinct ou.UserId,CAST(ou.UserSyncId AS nvarchar(255)) as UserSyncId,rc.Id as RecurringPaymentCustomerId,  
                        CASE
                            WHEN TRY_CAST(JSON_VALUE(rc.Tag, '$.card.exp_year') AS INT) < YEAR(GETDATE()) THEN 'Expired'
                            WHEN TRY_CAST(JSON_VALUE(rc.Tag, '$.card.exp_year') AS INT) = YEAR(GETDATE()) AND 
                            TRY_CAST(JSON_VALUE(rc.Tag, '$.card.exp_month') AS INT) < MONTH(GETDATE()) THEN 'Expired'
                        ELSE 'Valid'
                        END AS CardStatus,
                        CONCAT( JSON_VALUE(rc.Tag, '$.card.exp_month'),'/',
                        JSON_VALUE(rc.Tag, '$.card.exp_year')) as Expires,
                        Concat(JSON_VALUE(rc.Tag, '$.card.brand'), ' ****',
                        JSON_VALUE(rc.Tag, '$.card.last4'),' ',
                        JSON_VALUE(rc.Tag, '$.card.exp_month'),'/',
                        JSON_VALUE(rc.Tag, '$.card.exp_year') ) as CardName               
                    FROM RecurringPaymentCustomer rc
                    INNER JOIN RecurringPaymentPlan rpp 
                        ON rc.Id = rpp.CustomerId
                    INNER JOIN [User] ou ON rc.OwnerUserId = ou.UserId  
                         INNER JOIN Products_Default pd 
                       ON pd.DocId = rpp.ProductId  
                    where ou.UserSyncId IN @UserSyncId 
                    AND ISJSON(rc.Tag) = 1
                    AND paymentmethod = 'Stripe'
                    AND ISNULL(pd.OwnerId,0) =@OwnerId
                    UNION  
                    Select distinct ou.UserId,CAST(ou.UserSyncId AS nvarchar(255)) as UserSyncId,rc.Id as RecurringPaymentCustomerId,  
                        CASE
                            WHEN TRY_CAST(JSON_VALUE(rc.Tag, '$.card.exp_year') AS INT) < YEAR(GETDATE()) THEN 'Expired'
                            WHEN TRY_CAST(JSON_VALUE(rc.Tag, '$.card.exp_year') AS INT) = YEAR(GETDATE()) AND 
                            TRY_CAST(JSON_VALUE(rc.Tag, '$.card.exp_month') AS INT) < MONTH(GETDATE()) THEN 'Expired'
                        ELSE 'Valid'
                        END AS CardStatus,
                        CONCAT( JSON_VALUE(rc.Tag, '$.card.exp_month'),'/',
                        JSON_VALUE(rc.Tag, '$.card.exp_year')) as Expires,
                        Concat(JSON_VALUE(rc.Tag, '$.card.brand'), ' ****',
                        JSON_VALUE(rc.Tag, '$.card.last4'),' ',
                        JSON_VALUE(rc.Tag, '$.card.exp_month'),'/',
                        JSON_VALUE(rc.Tag, '$.card.exp_year') ) as CardName               
                    FROM RecurringPaymentCustomer rc
                    INNER JOIN RecurringPaymentPlan rpp 
                        ON rc.Id = rpp.CustomerId
                         INNER JOIN Products_Default pd 
                       ON pd.DocId = rpp.ProductId
                    INNER JOIN [User] ou 
                        ON rpp.ForEntityId = ou.MemberDocId  
                     where ou.UserSyncId IN @UserSyncId
                      AND paymentmethod = 'Stripe' 
                     AND ISJSON(rc.Tag) = 1 
                      AND rc.OwnerUserId <> ou.UserId
                    AND ISNULL(pd.OwnerId,0) =@OwnerId

            ";
            return (sql, queryParams);
        }

        private async Task PopulateUserCardsAsync(
            List<UserPaymentInfoDto> users,
            List<StoredCard> storedCards,
            List<InvoiceDetails> invoicedetails,
            List<RecurringPaymentCardInfo> storedStripeCards,
            bool isAdyenEnabled,
            string hostsystemid,
            CancellationToken cancellationToken)
        {
            foreach (var user in users)
            {
                var BillingDetails = invoicedetails
                    .FirstOrDefault(c => c.UserSyncId == user.UserSyncId);
                user.BillingDetails = BillingDetails;

                if (!isAdyenEnabled)
                {
                    var userCards = storedStripeCards
                        .Where(c => c.UserSyncId == user.UserSyncId).ToList();
                    user.UserCards = userCards;

                }
                else
                {

                    var userStoredCards = storedCards
                        .Where(c => c.UserSyncId == user.UserSyncId)
                        .ToList();

                    var externalCards = new List<StoredPaymentMethodResource>();

                    foreach (var metadata in storedCards
                            .Where(c => c.UserSyncId == user.UserSyncId)
                            .Select(c => c.Metadata)
                            .Where(m => !string.IsNullOrWhiteSpace(m))
                            .Distinct())
                    {
                        try
                        {
                            var finalMetadata = metadata.StartsWith("cus_", StringComparison.OrdinalIgnoreCase)
                                ? metadata
                                : $"{hostsystemid}.{user.UserSyncId?.ToLowerInvariant()}";

                            var shopperResult = await _mediator.Send(
                                new GetSavedCardDetailsQuery(metadata),
                                cancellationToken);

                            if (shopperResult?.StoredPaymentMethods?.Any() == true)
                            {
                                externalCards.AddRange(
                                    shopperResult.StoredPaymentMethods
                                        .Where(external => !string.IsNullOrEmpty(external?.Id))
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }
                    }

                    var matchedCards = (from external in externalCards
                                        join stored in userStoredCards
                                            on external.Id equals stored.Tag
                                        select new RecurringPaymentCardInfo
                                        {
                                            UserSyncId = user.UserSyncId,
                                            RecurringPaymentCustomerId = stored.RecurringPaymentCustomerId ?? 0,
                                            Expires = $"{external.ExpiryMonth}/{external.ExpiryYear}",
                                            CardName = $"{external.Brand} ****{external.LastFour} ({external.ExpiryMonth}/{external.ExpiryYear})"
                                        }).ToList();
                    user.UserCards = matchedCards;
                }
            }
        }
    }
}
