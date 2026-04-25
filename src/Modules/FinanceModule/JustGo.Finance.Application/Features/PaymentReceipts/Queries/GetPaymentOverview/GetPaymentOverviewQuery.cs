using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentOverview
{
    public class GetPaymentOverviewQuery : IRequest<PaymentOverviewDto>
    {
        public GetPaymentOverviewQuery(Guid merchantId, Guid paymentId)
        {
            PaymentId = paymentId;
            MerchantId = merchantId;
        }

        public Guid MerchantId { get; set; }
        public Guid PaymentId { get; set; }
    }

}
