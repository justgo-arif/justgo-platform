using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.FamilyUpdateManager
{
    public class FamilyUpdateManagerCommand : IRequest<int>
    {
        public Guid RecordGuid { get; set; }
        public bool MakeManager { get; set; } = false;
    }
}
