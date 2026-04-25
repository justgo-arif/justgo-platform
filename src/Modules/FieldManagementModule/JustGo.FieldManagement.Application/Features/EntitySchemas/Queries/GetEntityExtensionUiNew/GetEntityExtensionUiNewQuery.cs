using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiNew;

public class GetEntityExtensionUiNewQuery : IRequest<List<EntityExtensionUI>>
{
    public GetEntityExtensionUiNewQuery(string ownerType, string ownerId, string extensionArea, int extensionEntityId)
    {
        OwnerType = ownerType;
        OwnerId = ownerId;
        ExtensionArea = extensionArea;
        ExtensionEntityId = extensionEntityId;
    }

    public string OwnerType { get; set; }
    public string OwnerId { get; set; }
    public string ExtensionArea { get; set; }
    public int ExtensionEntityId { get; set; }
}
