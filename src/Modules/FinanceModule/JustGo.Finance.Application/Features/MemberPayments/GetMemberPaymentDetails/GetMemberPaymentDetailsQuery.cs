using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentDetails
{
    public class GetMemberPaymentDetailsQuery : IRequest<PaymentDetailsVM>
    {
        public GetMemberPaymentDetailsQuery(Guid memberId, Guid paymentId)
        {
            MemberId = memberId;
            PaymentId = paymentId;
        }
        public Guid MemberId { get; set; } 
        public Guid PaymentId { get; set; }
    }
}
