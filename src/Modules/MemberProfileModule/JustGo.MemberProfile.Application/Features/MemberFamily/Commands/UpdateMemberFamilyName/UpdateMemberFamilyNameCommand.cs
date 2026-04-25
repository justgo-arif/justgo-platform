using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.UpdateMemberFamilyName
{
    public class UpdateMemberFamilyNameCommand : IRequest<int>
    {
        public required Guid FamilySyncGuid { get; set; }
        public required string FamilyName { get; set; } = string.Empty;

    }
}
