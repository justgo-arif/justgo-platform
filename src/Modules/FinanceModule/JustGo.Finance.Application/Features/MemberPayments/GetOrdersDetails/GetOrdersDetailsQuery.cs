using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.MemberPayments.GetOrdersDetails
{

    public class GetOrdersDetailsQuery : IRequest<PaymentDetailsVM>
    {
        public GetOrdersDetailsQuery(Guid merchantId, Guid paymentId)
        {
            MerchantId = merchantId;
            PaymentId = paymentId;
        }
        public Guid MerchantId { get; set; }
        public Guid PaymentId { get; set; }
    }

}
