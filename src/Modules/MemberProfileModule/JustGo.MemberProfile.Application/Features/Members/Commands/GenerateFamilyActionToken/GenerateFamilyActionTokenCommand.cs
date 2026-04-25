using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.GenerateFamilyActionToken
{
    public class GenerateFamilyActionTokenCommand : IRequest<int>
    {
        public int FamilyDocId { get; set; }
        public int InitiateMemberDocId { get; set; }
        public int TargetMemberDocId { get; set; }
        public string Url { get; set; }
    }
}
