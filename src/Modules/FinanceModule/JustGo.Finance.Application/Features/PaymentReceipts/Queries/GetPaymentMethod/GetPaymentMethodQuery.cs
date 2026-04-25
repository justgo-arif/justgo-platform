using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentMethod
{

    public class GetPaymentMethodQuery : IRequest<PaymentMethod?>
    {
        public GetPaymentMethodQuery(Guid paymentId)
        {
            PaymentId = paymentId;
        }
        public Guid PaymentId { get; set; }
    }


}
