using AuthModule.Application.DTOs.Stores;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using Microsoft.AspNetCore.StaticFiles;

namespace AuthModule.Application.Features.Files.Queries.DownloadFile;

public class DownloadTempFileQueryHandler : IRequestHandler<DownloadTempFileQuery, DownloadTempFileResultDto>
{
    private readonly IAzureBlobFileService _fileSystemService;

    public DownloadTempFileQueryHandler(IAzureBlobFileService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public async Task<DownloadTempFileResultDto> Handle(DownloadTempFileQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return new DownloadTempFileResultDto
            {
                Success = false,
                ErrorMessage = "Path is required"
            };
        }

        var normalizedPath = Path.Combine("Temp", request.Path).Replace("\\", "/");
        var mappedPath = await _fileSystemService.MapPath(normalizedPath);

        if (!await _fileSystemService.Exists(mappedPath, cancellationToken))
        {
            return new DownloadTempFileResultDto
            {
                Success = false,
                ErrorMessage = "File not found"
            };
        }

        using var stream = await _fileSystemService.DownloadFileAsync(mappedPath, cancellationToken);
        if (stream == null)
        {
            return new DownloadTempFileResultDto
            {
                Success = false,
                ErrorMessage = "File not found"
            };
        }
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var fileBytes = ms.ToArray();
        var fileName = Path.GetFileName(mappedPath);

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new DownloadTempFileResultDto
        {
            Success = true,
            FileBytes = fileBytes,
            FileName = fileName,
            ContentType = contentType
        };
    }
}
