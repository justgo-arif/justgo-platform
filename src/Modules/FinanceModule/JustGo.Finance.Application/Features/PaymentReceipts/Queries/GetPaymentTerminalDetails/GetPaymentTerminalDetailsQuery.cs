using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentTerminalDetails
{

    public class GetPaymentTerminalDetailsQuery : IRequest<PaymentTerminalDetails>
    {

        public GetPaymentTerminalDetailsQuery(Guid paymentId)
        {
            PaymentId = paymentId;
        }
        public Guid PaymentId { get; set; }
    }

}
