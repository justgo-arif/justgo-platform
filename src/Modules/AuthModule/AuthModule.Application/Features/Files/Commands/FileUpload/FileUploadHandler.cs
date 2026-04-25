using AuthModule.Application.DTOs.Stores;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.Files.Commands.FileUpload;

public class FileUploadHandler : IRequestHandler<FileUploadCommand, UploadedFileInfo>
{
    private readonly IAzureBlobFileService _azureBlobFileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public FileUploadHandler(IAzureBlobFileService azureBlobFileService, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _azureBlobFileService = azureBlobFileService;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<UploadedFileInfo> Handle(FileUploadCommand request, CancellationToken cancellationToken)
    {
        var fileType = Path.GetExtension(request.File.FileName).ToLower();
        var blobFileName = $"{Guid.NewGuid()}{fileType}";

        var destBlobPath = "";
        if (request.Module.ToLower() == "user" && request.UserSyncId.HasValue)
        {
            var user = await _mediator.Send(new GetUserByUserSyncIdQuery((Guid)request.UserSyncId), cancellationToken);
            destBlobPath = await _azureBlobFileService.MapPath($"{ResolveUploadPath(request.Module, user.Userid)}{blobFileName}");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(destBlobPath))
            {
                throw new InvalidOperationException("Uploaded path could not be resolved.");
            }

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream, cancellationToken);
                fileBytes = memoryStream.ToArray();
            }

            if (fileBytes.Length == 0)
            {
                throw new InvalidOperationException("File stream was empty or already consumed.");
            }

            await _azureBlobFileService.UploadFileAsync(destBlobPath, fileBytes, FileMode.Create, cancellationToken);

            if (request.IsThumbnailNeeded)
            {
                await _azureBlobFileService.CreateThumbnailAsync(destBlobPath);
            }

            return new UploadedFileInfo 
            { 
                FilePath = destBlobPath, 
                FileName = blobFileName
            };
        }
        catch (Exception ex)
        {
            try
            {
                await _azureBlobFileService.DeleteFileAsync(destBlobPath, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                throw new Exception($"Failed to delete uploaded file during rollback: {deleteEx.Message}");
            }
            throw new InvalidOperationException($"Failed to create attachment: {ex.Message}", ex);
        }
    }

    private string ResolveUploadPath(string module, int userId = 0)
    {
        return module.ToLower() switch
        {
            "user" => $"~/Store/User/{userId}/",
            _ => ""
        };
    }
}