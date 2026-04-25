using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentBreakdown
{
    public class GetMemberPaymentBreakdownHandler : IRequestHandler<GetMemberPaymentBreakdownQuery, PaymentBreakdown>
    {
        private readonly LazyService<IReadRepository<PaymentBreakdown>> _readRepository;
        private readonly IMediator _mediator;

        public GetMemberPaymentBreakdownHandler(LazyService<IReadRepository<PaymentBreakdown>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentBreakdown> Handle(GetMemberPaymentBreakdownQuery request, CancellationToken cancellationToken)
        {
            var memberdocid = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.MemberId), cancellationToken);
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);

            var queryParams = new DynamicParameters();
            queryParams.Add("MemberDocId", memberdocid);
            queryParams.Add("DocId", docId);

            var sql = @" Select prd.DocId
                        ,MIN(ISNULL(prbr.Amount,0)) as PaymentAmount
                        ,MIN(ISNULL(prbr.Transactionfee,0)) as PaymentFee
                        ,SUM(ISNULL(rpb.Amount,0)) as RefundAmount, 
                        MIN(ISNULL(prbr.Amount,0))-SUM(ISNULL(rpb.Amount,0)) as NetAmount
                        FROM PaymentReceipts_Breakdownbyrecipient prbr 
                        INNER JOIN PaymentReceipts_Default prd ON prd.DocId = prbr.DocId
                        LEFT JOIN PaymentReceipts_Default rfp on prd.DocId = rfp.OriginalReceiptDocid
                        Left Join PaymentReceipts_Breakdownbyrecipient rpb on rfp.DocId= rpb.DocId AND prbr.Marchentid = rpb.Marchentid
                        INNER JOIN [User] u on prd.Paymentuserid = u.Userid
                        Where prd.DocId =@DocId  
                        AND u.MemberDocId = @MemberDocId
                        Group By prd.DocId";

            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParams, null, "text");
            if (result is null)
                throw new InvalidOperationException("Payment breakdown not found for the provided parameters.");
            return result;
        }
    }
}
