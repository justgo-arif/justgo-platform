using AuthModule.Application.DTOs.Attachments;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Queries.GetAttachments;

public class GetAttachmentsQuery : IRequest<List<AttachmentDto>>
{
    public GetAttachmentsQuery(int entityType, Guid entityId, string module)
    {
        EntityType = entityType;
        EntityId = entityId;
        Module = module;
    }

    public int EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Module { get; set; }
}
