using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.AddFamilyMember
{
    public class AddFamilyMemberCommand : IRequest<int>
    {
        public string Name { get; set; }
        public int FamilyDocId { get; set; }
        public int ClubDocId { get; set; }
        public int UserId { get; set; }
        public string MemberDocIds { get; set; }
    }
}
