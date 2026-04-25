using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.MemberDetailsMenu
{
    public class GetMemberDetailsMenuQuery : IRequest<List<EntityExtensionUI>>
    {
        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
        public string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; } = 0;
    }
}
