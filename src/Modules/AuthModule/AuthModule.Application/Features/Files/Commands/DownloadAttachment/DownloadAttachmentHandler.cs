using AuthModule.Application.DTOs.Stores;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Files;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using Microsoft.AspNetCore.StaticFiles;

namespace AuthModule.Application.Features.Files.Commands.DownloadAttachment;

public class DownloadAttachmentHandler : IRequestHandler<DownloadAttachmentCommand, DownloadFileStreamDto>
{
    private readonly IAttachmentService _attachmentService;
    private readonly IAzureBlobFileService _azureBlobFileService;
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public DownloadAttachmentHandler(IAttachmentService attachmentService, IAzureBlobFileService azureBlobFileService)
    {
        _attachmentService = attachmentService;
        _azureBlobFileService = azureBlobFileService;
    }

    public async Task<DownloadFileStreamDto> Handle(DownloadAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentService.GetAttachment(request.AttachmentId, request.Module, cancellationToken);
        var destBlobPath = await _azureBlobFileService.MapPath($"~/store/{request.Module}_attachments/{request.EntityId}/{attachment.GeneratedName}");
        if (!await _azureBlobFileService.Exists(destBlobPath, cancellationToken))
        {
            throw new NotFoundException($"File not found in storage: {attachment.Name}");
        }

        var stream = await _azureBlobFileService.DownloadFileAsync(destBlobPath, cancellationToken);
        if (stream is null)
        {
            throw new InvalidOperationException($"Failed to download file: {attachment.Name}");
        }

        if (!_contentTypeProvider.TryGetContentType(attachment.GeneratedName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new DownloadFileStreamDto
        {
            FileStream = stream,
            FileName = attachment.Name,
            ContentType = contentType,
            FileSize = attachment.Size
        };
    }
}