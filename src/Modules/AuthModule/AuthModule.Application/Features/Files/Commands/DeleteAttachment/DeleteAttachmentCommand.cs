using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Commands.DeleteAttachment;

public class DeleteAttachmentCommand : IRequest<int>
{
    public DeleteAttachmentCommand(Guid attachmentId, string module, Guid entityId)
    {
        AttachmentId = attachmentId;
        Module = module;
        EntityId = entityId;
    }

    public Guid AttachmentId { get; set; }
    public string Module { get; set; }
    public Guid EntityId { get; set; }
}