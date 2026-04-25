using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentCardInfo
{


    public class GetPaymentCardInfoQuery : IRequest<PaymentCardInfo>
    {
        public GetPaymentCardInfoQuery(Guid paymentId)
        {
            PaymentId = paymentId;
        }
        public Guid PaymentId { get; set; }
    }

}
