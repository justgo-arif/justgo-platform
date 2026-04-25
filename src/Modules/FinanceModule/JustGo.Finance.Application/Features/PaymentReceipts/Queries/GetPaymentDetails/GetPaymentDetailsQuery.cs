using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentDetails
{

    public class GetPaymentDetailsQuery : IRequest<PaymentDetailsVM>
    {
        public GetPaymentDetailsQuery(Guid merchantId, Guid paymentId)
        {
            MerchantId = merchantId;
            PaymentId = paymentId;
        }
        public Guid MerchantId { get; set; }
        public Guid PaymentId { get; set; }
    }

}
