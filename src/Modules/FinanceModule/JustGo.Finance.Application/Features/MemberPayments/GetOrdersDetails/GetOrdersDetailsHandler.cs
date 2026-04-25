using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentBreakdown;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentCardInfo;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.MemberPayments.GetOrdersDetails
{

    public class GetOrdersDetailsHandler : IRequestHandler<GetOrdersDetailsQuery, PaymentDetailsVM>
    {

        private readonly IMediator _mediator;

        public GetOrdersDetailsHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<PaymentDetailsVM> Handle(GetOrdersDetailsQuery request, CancellationToken cancellationToken)
        {
            var paymentDetailsVM = new PaymentDetailsVM();
            paymentDetailsVM.PaymentCardInfo = await _mediator.Send(new GetPaymentCardInfoQuery(request.PaymentId), cancellationToken) ?? new PaymentCardInfo();
            paymentDetailsVM.PaymentBreakdown = await _mediator.Send(new GetPaymentBreakdownQuery(request.MerchantId, request.PaymentId), cancellationToken) ?? new PaymentBreakdown();
            return paymentDetailsVM;
        }
    }

}
