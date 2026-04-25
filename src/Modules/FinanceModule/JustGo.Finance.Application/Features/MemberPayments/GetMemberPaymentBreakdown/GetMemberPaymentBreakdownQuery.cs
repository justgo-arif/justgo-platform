using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentBreakdown
{
    public class GetMemberPaymentBreakdownQuery : IRequest<PaymentBreakdown>
    {
        public GetMemberPaymentBreakdownQuery(Guid memberId, Guid paymentId)
        {
            MemberId = memberId;
            PaymentId = paymentId;
        }
        public Guid MemberId { get; set; }
        public Guid PaymentId { get; set; }
    }
}
