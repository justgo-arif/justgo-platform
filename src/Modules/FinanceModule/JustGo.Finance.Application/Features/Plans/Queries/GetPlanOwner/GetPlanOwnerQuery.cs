using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.MemberPaymentDTOs;

namespace JustGo.Finance.Application.Features.Plans.Queries.GetPlanOwner
{
    public class GetPlanOwnerQuery : IRequest<PlanInfo>
    {
        public GetPlanOwnerQuery(Guid planId, Guid memberId)
        {
            PlanId = planId;
            MemberId = memberId;
        }

        public Guid PlanId { get; set; }
        public Guid MemberId { get; set; }
    }
}
