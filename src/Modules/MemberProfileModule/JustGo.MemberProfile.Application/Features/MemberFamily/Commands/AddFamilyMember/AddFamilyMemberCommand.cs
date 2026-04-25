
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.AddFamilyMember;

public class AddFamilyMemberCommand : IRequest<int>
{
    public required Guid MemberSyncGuid { get; set; }
    public required Guid UserSyncGuid { get; set; }
    public bool IsNewMember { get; set; } = false;
    public bool LinkToClubs { get; set; } = false;
}
