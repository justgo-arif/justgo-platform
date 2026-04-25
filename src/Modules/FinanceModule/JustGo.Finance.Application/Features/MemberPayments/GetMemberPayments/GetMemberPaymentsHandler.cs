using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.MemberPaymentDTOs;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.GetFieldList;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPayments
{
    public class GetMemberPaymentsHandler : IRequestHandler<GetMemberPaymentsQuery, MemberPaymentVm>
    {
        private readonly LazyService<IReadRepository<MemberPayment>> _readRepository;
        private readonly IMediator _mediator;
        public GetMemberPaymentsHandler(LazyService<IReadRepository<MemberPayment>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<MemberPaymentVm> Handle(GetMemberPaymentsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;
            var allowedScopes = new[] { "all", "paymentid", "customername", "customermemberid" };

            var scopeKey = allowedScopes.Contains(request.ScopeKey?.ToLower())
                ? request.ScopeKey!.ToLower()
                : "all";
            if (request.PaymentMethods?.Count == 1 && request.PaymentMethods[0] == "string") request.PaymentMethods.Clear();
            var paymentInfoVM = new MemberPaymentVm();
            var memberdocid = await _mediator.Send(
                new GetDocIdBySyncGuidQuery(request.UserId), cancellationToken);
            var paymentFieldList = await _mediator.Send(
                           new GetFieldListQuery(72), cancellationToken);
            var paymentMethodFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Payment Method", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var originalReceiptFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Original Receipt DocId", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var paymentIdFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "PaymentId", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var paymentDateFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Date", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var paymentUserIdFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "PaymentUserId", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var grossAmountFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Gross Amount", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var currencyFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Currency", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var receiptStatusFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Receipt Status", StringComparison.OrdinalIgnoreCase))
                ?.Id; 
            var queryParameters = new DynamicParameters();
            queryParameters.Add("Memberdocid", memberdocid);
            queryParameters.Add("PageSize", request.PageSize);

            string CommonCondition = "";

            if (request.PaymentMethods?.Any() == true)
            {
                CommonCondition += $" AND prd.[Field_{paymentMethodFieldId}] IN @PaymentMethods ";
                queryParameters.Add("PaymentMethods", request.PaymentMethods);
            }

            if (request.StatusIds?.Any(x => x > 0) == true)
            {
                CommonCondition += " AND st.StateId IN @StatusIds ";
                queryParameters.Add("StatusIds", request.StatusIds.Where(x => x > 0));
            }

            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                var to = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                CommonCondition += $" AND prd.[Field_{paymentDateFieldId}] BETWEEN @FromDate AND @ToDate ";
                queryParameters.Add("FromDate", from);
                queryParameters.Add("ToDate", to);
            }
            if (!string.IsNullOrEmpty(request.SearchText) && !string.IsNullOrEmpty(scopeKey))
            {
                CommonCondition += @$" AND (
                    (@ScopeKey = 'all' AND (prd.[Field_{paymentIdFieldId}] LIKE @SearchText OR CONCAT(u.FirstName,' ',u.LastName) LIKE @SearchText OR u.MemberId LIKE @SearchText))
                    OR (@ScopeKey = 'paymentid' AND prd.[Field_{paymentIdFieldId}] LIKE @SearchText)
                    OR (@ScopeKey = 'customername' AND CONCAT(u.FirstName,' ',u.LastName) LIKE @SearchText)
                    OR (@ScopeKey = 'customermemberid' AND u.MemberId LIKE @SearchText)
                )";
                queryParameters.Add("ScopeKey", scopeKey.ToLower());
                queryParameters.Add("SearchText", $"%{request.SearchText}%");
            }

            string KeysetCondition = "";
            var lastPaymentId = request.LastPaymentId > 0
                ? request.LastPaymentId
                : null;

            if (lastPaymentId.HasValue)
            {
                KeysetCondition = " AND prd.DocId < @LastPaymentId";
                queryParameters.Add("LastPaymentId", lastPaymentId);
            }

            var sql = @$"
                SELECT TOP (@PageSize + 1)
                    d.SyncGuid as Id,
                    prd.DocId,
                    prd.[Field_{paymentIdFieldId}] As PaymentId, 
                    prd.[Field_{paymentDateFieldId}] as PaymentDate,
                    FORMAT(prd.[Field_{paymentDateFieldId}],'dd MMM yyyy, hh:mm tt') AS PaymentDateTime,
                    prd.[Field_{grossAmountFieldId}] as GrossAmount,
                    prd.[Field_{currencyFieldId}] as Currency,
                    Case 
                    When ISNULL(st.Name,'') = 'Paid' AND prd.[Field_{receiptStatusFieldId}] = 'Fully Refunded' Then prd.[Field_{receiptStatusFieldId}]
                    When ISNULL(st.Name,'') = 'Paid' AND prd.[Field_{receiptStatusFieldId}] = 'Partially Refunded' Then prd.[Field_{receiptStatusFieldId}]
                    When ISNULL(st.Name,'') = 'PendingCustomerAuthorization' Then 'Pending Customer Authorization'
                    When ISNULL(st.Name,'') = 'PendingApproval' Then 'Pending Approval'
                    When ISNULL(st.Name,'') = 'PendingPayment' Then 'Pending Payment'
                    ELSE ISNULL(st.Name,'') END
                    AS Status,                    
                    (select Count(DocId) from PaymentReceipts_Items Where DocId = prd.DocId) as TotalItems
                FROM  Document_12_72 prd  
                INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = prd.DocId
                INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                INNER JOIN [User] u ON u.Userid = prd.[Field_{paymentUserIdFieldId}]
                INNER JOIN Document d on prd.DocId = d.DocId
                WHERE u.MemberDocId = @Memberdocid
                    AND ISNULL(prd.Field_{originalReceiptFieldId},0) <= 0
                    {CommonCondition}
                    {KeysetCondition}
                ORDER BY prd.DocId DESC
            ";

            var payments = (await _readRepository.Value
                .GetListAsync(sql, cancellationToken, queryParameters, null, "text"))
                .ToList();

            var hasNext = payments.Count > request.PageSize;

            if (hasNext)
                payments.RemoveAt(payments.Count - 1);

            var lastItem = payments.LastOrDefault();
            var paymentDocIds = payments.Select(x => x.DocId).ToList();
            if (!paymentDocIds.Any())
            {
                paymentInfoVM.Payments = payments;
                paymentInfoVM.HasNextPage = false;
                paymentInfoVM.NextLastPaymentId = null;
                return paymentInfoVM;
            }
            var merchantSql = @$" DECLARE @DefaultLogo NVARCHAR(MAX);

                                SELECT TOP 1 
                                    @DefaultLogo = Value
                                FROM systemsettings  
                                WHERE itemkey = 'ORGANISATION.LOGO';

                                SELECT  Distinct
                                    pri.DocId as PaymentDocId,
                                    mpd.Name as MerchantName,
                                    CASE   
                                        WHEN mpl.Entityid IS NULL Then pri.MerchantId Else d.DocId END   as MerchantId,
                                    CASE 
                                        WHEN d.Location = 'Virtual' THEN '' 
                                        WHEN mpl.Entityid IS NULL THEN CONCAT( 'Store/Download?f=', @DefaultLogo ,'&t=OrganizationLogo' )
                                        ELSE Concat('store/download?f=', d.Location, '&t=repo&p=', d.DocId,'&p1=&p2=2')  
                                    END as MerchantImage
                                FROM PaymentReceipts_Items pri 
                                INNER JOIN MerchantProfile_Default mpd 
                                    ON pri.MerchantId = mpd.DocId 
                                LEFT JOIN MerchantProfile_Links mpl 
                                    ON mpd.DocId = mpl.docid 
                                LEFT JOIN Clubs_default d 
                                    ON mpl.EntityId = d.DocId 
                                Where pri.DocId IN @PaymentDocIds ";
            var merchants = await _readRepository.Value
                .GetListAsync<PaymentMerchantRowDto>(
                    merchantSql,
                    new { PaymentDocIds = paymentDocIds });

            var itemsql = $@"
                WITH ItemRanked AS
                (
                    SELECT 
                    pritms.DocId,
                    pritms.Productid AS ProductDocId,
                    ISNULL(pd.ProductReference,'') AS ProductReference,
                    Case When pd.Name like '%PC Product%' OR pd.Name like '%Console%' Then pritms.Description Else pd.Name END AS ProductName,
                    pritms.ForEntityType,
                    CASE 
                        WHEN pritms.ForEntityType = 'Asset' 
                            THEN ISNULL(ar.AssetName,'')
                        ELSE CONCAT(u.FirstName,' ',u.LastName)
                    END AS CustomerName,

                    CASE 
                        WHEN pritms.ForEntityType = 'Asset' 
                            THEN ISNULL(ar.AssetReference,'')
                        ELSE u.MemberId
                    END AS CustomerId,

                    pritms.Quantity,
                    ISNULL(pritms.Gross,0) AS Gross,

                    CASE 
                            WHEN pd.Location = 'Virtual' OR NULLIF(pd.Location,'') IS NULL THEN '' 
                            ELSE Concat('store/download?f=',  pd.Location,'&t=repo&p=',pd.DocId,'&p1=&p2=11' )  
                        END AS ProductImageURL,

                    ROW_NUMBER() OVER(PARTITION BY pritms.DocId ORDER BY pritms.DocId) AS rn

                FROM PaymentReceipts_Items pritms

                INNER JOIN Products_Default pd 
                    ON pritms.Productid = pd.DocId

                LEFT JOIN [User] u 
                    ON pritms.ForEntityId = u.MemberDocId
                    AND pritms.ForEntityType <> 'Asset'

                LEFT JOIN AssetRegisters ar
                    ON pritms.ForEntityId = ar.AssetId
                    AND pritms.ForEntityType = 'Asset'
                    WHERE pritms.DocId IN @PaymentDocIds
                )
                SELECT *
                FROM ItemRanked
                WHERE rn <= 3
                ORDER BY DocId, rn;
            ";
            var items = await _readRepository.Value
                .GetListAsync<MemberPaymentInfoRowDto>(
                    itemsql,
                    new { PaymentDocIds = paymentDocIds });


            var merchantLookup = merchants
                .GroupBy(x => x.PaymentDocId)
                .ToDictionary(g => g.Key, g => g.ToList());
            paymentInfoVM.Payments = payments;
            var itemLookup = items
                .GroupBy(x => x.DocId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var payment in payments)
            {
                if (merchantLookup.TryGetValue(payment.DocId, out var merchantRows))
                {
                    payment.Merchants = merchantRows.Select(m => new MemberPaymentMerchantVm
                    {
                        MerchantId = m.MerchantId,
                        MerchantName = m.MerchantName,
                        MerchantImage = m.MerchantImage
                    }).ToList();
                }
                if (itemLookup.TryGetValue(payment.DocId, out var itemRows))
                {
                    payment.Items = itemRows.Select(i => new MemberPaymentInfoDto
                    {
                        ProductDocId = i.ProductDocId,
                        ProductName = i.ProductName,
                        ProductReference = i.ProductReference,
                        CustomerName = i.CustomerName,
                        CustomerId = i.CustomerId,
                        Quantity = i.Quantity,
                        Gross = i.Gross,
                        ProductImageURL = i.ProductImageURL
                    }).ToList();
                }
            }
            paymentInfoVM.HasNextPage = hasNext;
            paymentInfoVM.NextLastPaymentId = hasNext ? lastItem?.DocId : null;
            return paymentInfoVM;
        }

    }
}
