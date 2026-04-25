using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.DeleteFamilyMember
{
    public class DeleteFamilyMemberCommand : IRequest<int>
    {
        public DeleteFamilyMemberCommand(int familyDocId, int memberDocId)
        {
            FamilyDocId = familyDocId;
            MemberDocId = memberDocId;
        }

        public int FamilyDocId { get; set; }
        public int MemberDocId { get; set; }
    }
}
