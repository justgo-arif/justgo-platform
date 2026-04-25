using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentOverview
{
    public class GetMemberPaymentOverviewQuery : IRequest<PaymentOverviewDto>
    {
        public GetMemberPaymentOverviewQuery(Guid memberId, Guid paymentId)
        {
            PaymentId = paymentId;
            MemberId = memberId;
        }

        public Guid MemberId { get; set; }
        public Guid PaymentId { get; set; }
    }
}
