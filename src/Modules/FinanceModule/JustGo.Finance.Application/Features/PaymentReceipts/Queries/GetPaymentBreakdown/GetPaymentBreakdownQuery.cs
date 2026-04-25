using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentBreakdown
{


    public class GetPaymentBreakdownQuery : IRequest<PaymentBreakdown>
    {
        public GetPaymentBreakdownQuery(Guid merchantId, Guid paymentId)
        {
            MerchantId = merchantId;
            PaymentId = paymentId;
        }
        public Guid MerchantId { get; set; }
        public Guid PaymentId { get; set; }
    }

}