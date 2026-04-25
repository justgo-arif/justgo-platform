using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentLog
{
    public class GetPaymentLogQuery : IRequest<List<PaymentLog>>
    {
        public GetPaymentLogQuery(Guid paymentId)
        {
            PaymentId = paymentId;
        }
        public Guid PaymentId { get; set; }


    }

}
