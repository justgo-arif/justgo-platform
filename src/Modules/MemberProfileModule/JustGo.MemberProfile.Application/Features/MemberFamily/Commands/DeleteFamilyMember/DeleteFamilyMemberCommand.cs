using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.DeleteFamilyMember;

public class DeleteFamilyMemberCommand : IRequest<string>
{
    public DeleteFamilyMemberCommand(int userFamilyId)
    {
        UserFamilyId = userFamilyId;
    }

    public int UserFamilyId { get; set; }
}
