using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentBreakdown;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentCardInfo;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentDetails
{
    public class GetMemberPaymentDetailsHandler : IRequestHandler<GetMemberPaymentDetailsQuery, PaymentDetailsVM>
    {

        private readonly IMediator _mediator;

        public GetMemberPaymentDetailsHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<PaymentDetailsVM> Handle(GetMemberPaymentDetailsQuery request, CancellationToken cancellationToken)
        {
            var paymentDetailsVM = new PaymentDetailsVM();
            paymentDetailsVM.PaymentCardInfo = await _mediator.Send(new GetPaymentCardInfoQuery(request.PaymentId), cancellationToken) ?? new PaymentCardInfo();
            paymentDetailsVM.PaymentBreakdown = await _mediator.Send(new GetMemberPaymentBreakdownQuery(request.MemberId, request.PaymentId), cancellationToken) ?? new PaymentBreakdown();
            return paymentDetailsVM;
        }
    }
}
