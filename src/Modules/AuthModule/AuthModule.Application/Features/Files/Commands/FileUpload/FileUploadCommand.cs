using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using AuthModule.Application.DTOs.Stores;

namespace AuthModule.Application.Features.Files.Commands.FileUpload;

public class FileUploadCommand : IRequest<UploadedFileInfo>
{
    public required string Module { get; set; }
    public required IFormFile File { get; set; }
    public bool IsThumbnailNeeded { get; set; } = false;
    public Guid? UserSyncId { get; set; }
}
