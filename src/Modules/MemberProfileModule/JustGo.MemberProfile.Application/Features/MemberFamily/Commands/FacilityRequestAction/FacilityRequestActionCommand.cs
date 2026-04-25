using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.FacilityRequestAction
{
    public class FamilyRequestActionCommand : IRequest<int>
    {
        public Guid RecordGuid { get; set; }
        public bool Accepted { get; set; }
    }
}