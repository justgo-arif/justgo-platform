using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentSummary
{
    public class GetPaymentSummaryQuery : IRequest<PaymentSummaryVM>
    {
        public Guid? MerchantId { get; set; }

        public Guid? MemberId { get; set; }

        public Guid PaymentId { get; set; }

        public RequestSource Source { get; set; }

        public GetPaymentSummaryQuery(Guid paymentId)
        {
            PaymentId = paymentId;
        }
    }
}
