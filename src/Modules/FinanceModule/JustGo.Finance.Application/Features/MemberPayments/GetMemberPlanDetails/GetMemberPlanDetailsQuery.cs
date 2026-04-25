using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPlanDetails
{
    public class GetMemberPlanDetailsQuery : IRequest<InstallmentResponse>
    {
        public Guid MemberId { get; set; }
        public Guid PlanId { get; set; }

        public GetMemberPlanDetailsQuery(Guid memberId, Guid planId)
        {
            MemberId = memberId;
            PlanId = planId;
        }
    }
}
