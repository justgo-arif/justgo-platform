using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;

namespace AuthModule.Application.Features.Files.Commands.CreateAttachment;

public class CreateAttachmentCommand : IRequest<int>
{
    public required int EntityType { get; set; }
    public required Guid EntityId { get; set; }
    public required string Module { get; set; }
    public required IFormFile File { get; set; }
}