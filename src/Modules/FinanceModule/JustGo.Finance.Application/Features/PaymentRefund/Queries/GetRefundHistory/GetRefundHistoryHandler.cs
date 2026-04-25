using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId;

namespace JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundHistory
{
    public class GetRefundHistoryHandler : IRequestHandler<GetRefundHistoryQuery, RefundInfoVM>
    {
        private readonly LazyService<IReadRepository<RefundInfoDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetRefundHistoryHandler(LazyService<IReadRepository<RefundInfoDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<RefundInfoVM> Handle(GetRefundHistoryQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string")
                request.SearchText = null;

            if (string.IsNullOrWhiteSpace(request.ColumnName) || request.ColumnName == "string")
                request.ColumnName = "ReferenceID";

            if (string.IsNullOrWhiteSpace(request.OrderBy) || request.OrderBy == "string")
                request.OrderBy = "ASC";

            var refundVM = new RefundInfoVM();

            string ownerCondition = "";
            var queryParameters = new DynamicParameters();
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            queryParameters.Add("DocId", docId);
            queryParameters.Add("PageNo", request.PageNo);
            queryParameters.Add("PageSize", request.PageSize);

            if (request.Source == RequestSource.Member)
            {
                if (!request.MemberId.HasValue)
                    throw new ArgumentException("MemberId is required for Member source.");

                int memberDocId = await _mediator.Send(
                    new GetDocIdBySyncGuidQuery(request.MemberId.Value), cancellationToken);

                ownerCondition = "AND u.MemberDocId = @OwnerId";
                queryParameters.Add("OwnerId", memberDocId);
            }
            else if (request.Source == RequestSource.Merchant)
            {
                if (!request.MerchantId.HasValue)
                    throw new ArgumentException("MerchantId is required for Merchant source.");

                int merchantIntId = await _mediator.Send(
                    new GetMerchantIdQuery(request.MerchantId.Value), cancellationToken);

                ownerCondition = "AND pritems.MerchantId = @OwnerId";
                queryParameters.Add("OwnerId", merchantIntId);
            }
            else
            {
                throw new ArgumentException("Invalid Source.");
            }

            if (!string.IsNullOrEmpty(request.SearchText))
            {
                ownerCondition += @" AND (prd.Paymentrefundid LIKE @SearchText 
                                    OR CONCAT(u.FirstName,' ',u.LastName) LIKE @SearchText 
                                    OR ReceiptStatus LIKE @SearchText)";
                queryParameters.Add("SearchText", $"%{request.SearchText}%");
            }

            var allowedOrders = new[] { "ASC", "DESC" };
            string orderBy = allowedOrders.Contains(request.OrderBy?.ToUpper())
                ? $"{request.ColumnName} {request.OrderBy.ToUpper()}"
                : "ReferenceID ASC";

            string sqlCount = @$"
            SELECT COUNT(*) as TotalCount
            FROM (
                SELECT prd.Paymentrefundid as ReferenceID
                FROM PaymentReceipts_Default prd
                INNER JOIN PaymentReceipts_Items pritems ON prd.DocId = pritems.DocId
                INNER JOIN [User] u ON prd.Paymentuserid = u.userid
                WHERE OriginalReceiptDocid = @DocId
                {ownerCondition}
                GROUP BY prd.Paymentrefundid
            ) r";

            var totalresultobj = await _readRepository.Value.GetSingleAsync(sqlCount, cancellationToken, queryParameters, null, "text");

            if (totalresultobj is null)
                throw new InvalidOperationException("Total count not found for the provided PaymentId.");

            refundVM.TotalCount = Convert.ToInt32(totalresultobj);

            if (refundVM.TotalCount == 0) return refundVM;

            refundVM.PageNo = request.PageNo;
            refundVM.PageSize = request.PageSize;

            string sql = @$"
            SELECT ReferenceID,
                   RefundedBy,
                   FORMAT(IssueDate,'dd MMMM yyyy') as IssueDate,
                   GrossAmount,
                   Status
            FROM (
                SELECT prd.Paymentrefundid as ReferenceID,
                       CONCAT(u.FirstName,' ',u.LastName) AS RefundedBy,
                       prd.Date AS IssueDate,
                       SUM(pritems.Gross) AS GrossAmount,
                       ReceiptStatus AS Status
                FROM PaymentReceipts_Default prd
                INNER JOIN PaymentReceipts_Items pritems ON prd.DocId = pritems.DocId
                INNER JOIN [User] u ON prd.Paymentuserid = u.userid
                WHERE OriginalReceiptDocid = @DocId
                {ownerCondition}
                GROUP BY prd.Paymentrefundid, CONCAT(u.FirstName,' ',u.LastName), prd.Date, ReceiptStatus
            ) r
            ORDER BY {orderBy}
            OFFSET (@PageNo - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";

            refundVM.RefundInfos = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            return refundVM;
        }
    }

}
