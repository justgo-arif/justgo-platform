using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.PaymentRefundDTOs;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId;

namespace JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundProduct
{

    public class GetRefundableItemsHandler : IRequestHandler<GetRefundableItemsQuery, List<RefundableItemDto>>
    {
        private readonly LazyService<IReadRepository<RefundableItemDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetRefundableItemsHandler(LazyService<IReadRepository<RefundableItemDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        } 
        public async Task<List<RefundableItemDto>> Handle(GetRefundableItemsQuery request, CancellationToken cancellationToken)
        {
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);

            string merchantFilter = "";
            string memberFilter = ""; 
            if (request.Source == RequestSource.Member)
            {
                if (!request.MemberId.HasValue)
                    throw new ArgumentException("MemberId is required for Member source.");

                var memberDocId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.MemberId.Value), cancellationToken);
                queryParameters.Add("MemberDocId", memberDocId);
                memberFilter = "AND ou.MemberDocId = @MemberDocId";
            }
            else if (request.Source == RequestSource.Merchant)
            {
                if (!request.MerchantId.HasValue)
                    throw new ArgumentException("MerchantId is required for Merchant source.");

                var merchantId = await _mediator.Send(new GetMerchantIdQuery(request.MerchantId.Value), cancellationToken);
                queryParameters.Add("MerchantId", merchantId);
                merchantFilter = "AND pritems.MerchantId = @MerchantId";
            }
            else
            {
                throw new ArgumentException("Invalid Source.");
            }
            var sql = $@"
        ;WITH RefundData AS (
            SELECT 
                pri.Refunditemrowid,
                pri.ForEntityId,
                pri.MerchantId,
                SUM(Gross) AS RefundAmount
            FROM PaymentReceipts_Default pd
            INNER JOIN PaymentReceipts_Items pri ON pd.DocId = pri.DocId
            INNER JOIN [User] ou ON pd.Paymentuserid = ou.userid
            WHERE pd.OriginalReceiptDocid = @DocId
            {merchantFilter}
            {memberFilter}
            GROUP BY pri.Refunditemrowid, pri.ForEntityId, pri.MerchantId
        )

        SELECT
            pritems.RowId,
            ISNULL(Productcode,'') AS Code,
            pritems.Quantity,
            pritems.Price AS UnitPrice,
            d.SyncGuid AS ProductId,
            pd.Name AS ItemName,
            pd.Description AS ItemDescription,
            COALESCE(
                NULLIF(CONCAT(u.FirstName,' ',u.LastName),''),
                cd.ClubName,
                td.TeamName,
                pritems.[Group]
            ) AS MemberName,
            COALESCE(
                md.MID,
                cd.ClubId,
                td.TeamID,
                pritems.[Group]
            ) AS MemberId,
            ISNULL(pritems.Imageurl,'') ProfilePicURL,
            pritems.Gross AS OriginalAmount,
            rd.RefundAmount,
            pritems.Gross - ISNULL(rd.RefundAmount,0) AS AmountRemaining,
            pritems.Gross - ISNULL(rd.RefundAmount,0) AS AmountToRefund,
            pritems.ForEntityId AS ForEntityDocId
        FROM PaymentReceipts_Items pritems
        INNER JOIN PaymentReceipts_Default prd ON pritems.DocId = prd.DocId
        INNER JOIN [User] ou ON prd.Paymentuserid = ou.userid
        INNER JOIN Products_Default pd ON pritems.Productid = pd.DocId
        INNER JOIN Document d ON pd.DocId = d.DocId
        LEFT JOIN [User] u ON pritems.Forentityid = u.MemberDocId
        LEFT JOIN Members_Default md ON u.MemberDocId = md.DocId
        LEFT JOIN Clubs_Default cd ON pritems.Forentityid = cd.DocId
        LEFT JOIN Teams_Default td ON pritems.Forentityid = td.DocId
        LEFT JOIN RefundData rd 
            ON pritems.RowId = rd.Refunditemrowid 
            AND pritems.ForEntityId = rd.ForEntityId 
            AND pritems.MerchantId = rd.MerchantId
        WHERE pritems.DocId = @DocId
        {merchantFilter}
        {memberFilter}";

            return (await _readRepository.Value
                .GetListAsync(sql, cancellationToken, queryParameters, null, "text"))
                .ToList();
        }
    }

}
