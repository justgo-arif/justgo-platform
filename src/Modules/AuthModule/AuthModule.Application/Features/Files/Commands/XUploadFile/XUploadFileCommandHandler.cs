using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGoAPI.Shared.Helper;

namespace AuthModule.Application.Features.Files.Commands.XUploadFile;

public class XUploadFileCommandHandler : IRequestHandler<XUploadFileCommand, string>
{
    private readonly IAzureBlobFileService _fileSystemService;

    public XUploadFileCommandHandler(IAzureBlobFileService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public async Task<string> Handle(XUploadFileCommand request, CancellationToken cancellationToken)
    {
        var path = Utilities.ResolveTempUploadPath(request.T, request.File.FileName);
        if (string.IsNullOrEmpty(path))
            throw new Exception($"Invalid parameter value {request.T},{request.P}");

        var mappedPath = await _fileSystemService.MapPath(path);

        // Use the IFormFile overload for simplicity
        await _fileSystemService.UploadFileAsync(mappedPath, request.File, cancellationToken);

        return path;
    }
}
