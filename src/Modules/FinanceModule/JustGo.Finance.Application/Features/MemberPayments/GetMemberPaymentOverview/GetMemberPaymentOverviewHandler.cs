using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.GetFieldList;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentOverview
{
    public class GetMemberPaymentOverviewHandler : IRequestHandler<GetMemberPaymentOverviewQuery, PaymentOverviewDto>
    {
        private readonly LazyService<IReadRepository<PaymentOverviewDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetMemberPaymentOverviewHandler(LazyService<IReadRepository<PaymentOverviewDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentOverviewDto> Handle(GetMemberPaymentOverviewQuery request, CancellationToken cancellationToken)
        {
            var memberdocid = await _mediator.Send(
                new GetDocIdBySyncGuidQuery(request.MemberId), cancellationToken);
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            var paymentFieldList = await _mediator.Send(
                           new GetFieldListQuery(72), cancellationToken);
            var paymentMethodFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Payment Method", StringComparison.OrdinalIgnoreCase))
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
            var receiptStatusFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Receipt Status", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var paymentPaidTimeFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "PaymentPaidTime", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var currencyFieldId = paymentFieldList
                .FirstOrDefault(x =>
                    string.Equals(x.Name?.Trim(), "Currency", StringComparison.OrdinalIgnoreCase))
                ?.Id;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);
            queryParameters.Add("Memberdocid", memberdocid);

            var sql = @$"
                ;WITH RefundAmount AS (
                    SELECT 
                        SUM(Gross) AS Amount, 
                        prd.OriginalReceiptDocid AS DocId 
                    FROM PaymentReceipts_Default prd  
                    INNER JOIN PaymentReceipts_Items pritems ON prd.DocId = pritems.DocId
                    WHERE prd.OriginalReceiptDocid = @DocId 
                    GROUP BY prd.OriginalReceiptDocid 
                ),
                PaymentsData AS (
                    SELECT 
                        prd.DocId, 
                        prd.[Field_{paymentIdFieldId}] AS PaymentId,
                        CONCAT(u.FirstName,' ',u.LastName) AS CustomerName,
                        u.MemberId AS CustomerId,
                        u.EmailAddress AS CustomerEmailAddress,
                        prd.[Field_{paymentMethodFieldId}] AS PaymentMethod, 
                        FORMAT(prd.[Field_{paymentDateFieldId}],'dd MMM yyyy, hh:mm tt') AS PaymentDate,
                        prd.[Field_{paymentPaidTimeFieldId}] AS Paymentpaidtime,
                        prd.[Field_{receiptStatusFieldId}] AS ReceiptStatus,
                        prd.[Field_{currencyFieldId}] AS Currency,
                        st.Name AS [Status],
                        CASE WHEN prd.[Field_{paymentIdFieldId}] LIKE '%RR%' THEN 'Refund' ELSE 'Payment' END AS PaymentType,
                        (case 
                            when u.ProfilePicURL='' or u.ProfilePicURL is null 
                            then ''
                            else 'Store/download?f='+u.ProfilePicURL +'&t=user&p=' +Convert(varchar(10),u.Userid) end) 
                                AS ProfilePicURL,
                        SUM(Gross) OVER(PARTITION BY prd.DocId) AS Amount 
                    FROM Document_12_72 prd  
                    INNER JOIN PaymentReceipts_Items pritems ON prd.DocId = pritems.DocId
                    INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = prd.DocId
                    INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                    INNER JOIN [User] u ON u.Userid = prd.[Field_{paymentUserIdFieldId}]
                    WHERE prd.DocId = @DocId AND u.MemberDocId = @MemberDocId
                )
                SELECT Distinct
                    pd.PaymentId,
                    pd.CustomerName,
                    pd.CustomerId,
                    pd.CustomerEmailAddress,
                    pd.PaymentMethod,
                    pd.PaymentDate,
                    pd.Paymentpaidtime,
                    pd.ReceiptStatus,
                    pd.Status,
                    pd.PaymentType,
                    pd.ProfilePicURL,
                    pd.Currency,
                    pd.Amount AS TotalAmount,
                    ISNULL(ra.Amount, 0) AS RefundedAmount,
                    CASE WHEN  pd.Amount - ISNULL(ra.Amount, 0) > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsRefundable
                FROM PaymentsData pd
                LEFT JOIN RefundAmount ra ON pd.DocId = ra.DocId";
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text") ?? new PaymentOverviewDto();

        }
    }
}
