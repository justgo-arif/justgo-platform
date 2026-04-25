using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.MemberPaymentDTOs;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPlans
{
    public class GetMemberPlansQuery : IRequest<PlansPageVM>
    {
        public GetMemberPlansQuery(Guid memberId, string? status)
        {
            MemberId = memberId;
            Status = status;
        }
        public Guid MemberId { get; set; }
        public string? Status { get; set; }
    }
}
