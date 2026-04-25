using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentOverview
{

    public class GetPaymentOverviewQueryHandler : IRequestHandler<GetPaymentOverviewQuery, PaymentOverviewDto>
    {
        private readonly LazyService<IReadRepository<PaymentOverviewDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetPaymentOverviewQueryHandler(LazyService<IReadRepository<PaymentOverviewDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentOverviewDto> Handle(GetPaymentOverviewQuery request, CancellationToken cancellationToken)
        {
            var merchantId = await _mediator.Send(new GetMerchantIdQuery(request.MerchantId), cancellationToken);
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);
            queryParameters.Add("Merchantid", merchantId);

            var sql = @"
                ;WITH RefundAmount AS (
                    SELECT 
                        SUM(Gross) AS Amount, 
                        prd.OriginalReceiptDocid AS DocId,
                        pritems.Merchantid  
                    FROM PaymentReceipts_Default prd  
                    INNER JOIN PaymentReceipts_Items pritems ON prd.DocId = pritems.DocId
                    WHERE prd.OriginalReceiptDocid = @DocId
                      AND pritems.Merchantid = @Merchantid
                    GROUP BY prd.OriginalReceiptDocid, pritems.Merchantid
                ),
                PaymentsData AS (
                    SELECT 
                        prd.DocId, 
                        prd.[Field_487] AS PaymentId,
                        CONCAT(u.FirstName,' ',u.LastName) AS CustomerName,
                        u.MemberId AS CustomerId,
                        u.EmailAddress AS CustomerEmailAddress,
                        prd.[Field_494] AS PaymentMethod, 
                        FORMAT(prd.[Field_479],'dd MMM yyyy, hh:mm tt') AS PaymentDate,
                        prd.[Field_2487] AS Paymentpaidtime,
                        prd.[Field_495] AS ReceiptStatus,
                        prd.[Field_2287] AS Currency,
                        st.Name AS [Status],
                        CASE WHEN prd.[Field_487] LIKE '%RR%' THEN 'Refund' ELSE 'Payment' END AS PaymentType,
                        (case 
                            when u.ProfilePicURL='' or u.ProfilePicURL is null 
                            then ''
                            else 'Store/download?f='+u.ProfilePicURL +'&t=user&p=' +Convert(varchar(10),u.Userid) end) 
                             AS ProfilePicURL,
                        SUM(Gross) OVER(PARTITION BY prd.DocId, pritems.Merchantid) AS Amount 
                    FROM Document_12_72 prd  
                    INNER JOIN PaymentReceipts_Items pritems ON prd.DocId = pritems.DocId
                    INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = prd.DocId
                    INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                    INNER JOIN [User] u ON u.Userid = prd.[Field_478]
                    WHERE prd.DocId = @DocId AND pritems.Merchantid = @Merchantid
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
                    CASE WHEN pd.Amount - ISNULL(ra.Amount, 0) > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsRefundable
                FROM PaymentsData pd
                LEFT JOIN RefundAmount ra ON pd.DocId = ra.DocId";
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text") ?? new PaymentOverviewDto();

        }
    }
}
