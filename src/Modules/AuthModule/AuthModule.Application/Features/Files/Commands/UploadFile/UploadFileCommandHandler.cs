using AuthModule.Application.DTOs.Stores;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGoAPI.Shared.Helper;

namespace AuthModule.Application.Features.Files.Commands.UploadFile;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadFileResultDto>
{
    private readonly IAzureBlobFileService _azureBlobFileService;

    public UploadFileCommandHandler(IAzureBlobFileService azureBlobFileService)
    {
        _azureBlobFileService = azureBlobFileService;
    }

    public async Task<UploadFileResultDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (!Utilities.IsFileAllowed(request.FileName))
        {
            return new UploadFileResultDto
            {
                Success = false,
                ErrorMessage = "File Type not allowed"
            };
        }

        string entityType = request.EntityType;
        string clientUploaderRef = request.ClientUploaderRef;
        string useTemp = request.UseTemp;
        string successReturnAction = request.SuccessReturnAction;
        string errorCallBackMethod = request.ErrorCallBackMethod;
        string customStorePath = request.CustomStorePath ?? string.Empty;

        // Handle "custom" entity type
        if (!string.IsNullOrEmpty(entityType) && entityType.Contains("custom"))
        {
            var parts = entityType.Split('|');
            if (parts.Length > 1)
            {
                customStorePath = parts[1];
                entityType = "custom";
            }
        }

        string path;
        try
        {
            if (useTemp == "USE_TEMP")
            {
                path = Utilities.ResolveTempUploadPath(entityType, Utilities.GetUniqName(request.FileName));
            }
            else
            {
                path = Utilities.ResolveDirectUploadPath(
                    entityType,
                    Utilities.GetUniqName(
                        entityType == "fm_content" || entityType == "email" ? "" : request.FileName,
                        entityType == "mailattachment" || entityType == "fieldmanagementattach" || entityType == "justgobookingattachment" || entityType == "competitionattachment",
                        entityType == "fm_content" || entityType == "email" ? Path.GetExtension(request.FileName) : ".png"
                    ),
                    customStorePath
                );
            }

            var serverPath = await _azureBlobFileService.MapPath(path);
            await _azureBlobFileService.UploadFileAsync(serverPath, request.FileBytes, FileMode.Create, cancellationToken);



            var downloadUrl = $"/store/downloadPublic?f={Path.GetFileName(path)}&t={entityType}";

            // Return result based on uploader type
            switch (clientUploaderRef)
            {
                case "froala":
                    return new UploadFileResultDto
                    {
                        Success = true,
                        Link = successReturnAction + downloadUrl
                    };
                case "dropzone":
                    if (useTemp == "USE_TEMP")
                    {
                        return new UploadFileResultDto
                        {
                            Success = true,
                            DownloadUrl = $"store/downloadTemp?path={path.Substring(path.IndexOf("/") + 1)}"
                        };
                    }
                    else
                    {
                        return new UploadFileResultDto
                        {
                            Success = true,
                            DownloadUrl = $"store/downloadPublic?f={path.Substring(("media/images/" + entityType + "/").Length)}&t={entityType}"
                        };
                    }
                default:
                    return new UploadFileResultDto
                    {
                        Success = true,
                        DownloadUrl = downloadUrl
                    };
            }
        }
        catch (Exception ex)
        {
            switch (clientUploaderRef)
            {
                case "froala":
                    return new UploadFileResultDto
                    {
                        Success = false,
                        ErrorCode = 1,
                        ErrorMessage = ex.Message
                    };
                default:
                    return new UploadFileResultDto
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
            }
        }
    }

}
