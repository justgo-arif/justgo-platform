using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetBillingAddress
{
    public class GetBillingAddressQuery : IRequest<Address>
    {
        public GetBillingAddressQuery(Guid paymentId)
        {
            PaymentId = paymentId;
        }

        public Guid PaymentId { get; set; }
    }
}
