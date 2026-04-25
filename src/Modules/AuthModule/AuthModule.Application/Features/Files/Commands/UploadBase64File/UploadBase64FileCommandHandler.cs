using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGoAPI.Shared.Helper;

namespace AuthModule.Application.Features.Files.Commands.UploadBase64File;

public class UploadBase64FileCommandHandler : IRequestHandler<UploadBase64FileCommand, string>
{
    private readonly IAzureBlobFileService _fileSystemService;

    public UploadBase64FileCommandHandler(IAzureBlobFileService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public async Task<string> Handle(UploadBase64FileCommand request, CancellationToken cancellationToken)
    {
        string useCache = request.P;
        string format = request.P1;
        string entityType = request.T;
        string customStorePath = "";

        if (entityType.IndexOf("custom") > -1)
        {
            customStorePath = entityType.Split('|')[1];
            entityType = "custom";
        }

        string fileName = Utilities.GetUniqName(null, true, format);
        string filePath = useCache == "USECACHE"
            ? Utilities.ResolveTempUploadPath(entityType, fileName)
            : Utilities.ResolveDirectUploadPath(entityType, fileName, customStorePath);

        try
        {
            var mappedPath = await _fileSystemService.MapPath(filePath);

            // Decode base64 and save
            var base64Data = request.Base64String.Contains(",")
                ? request.Base64String.Substring(request.Base64String.IndexOf(",") + 1)
                : request.Base64String;
            var bytes = Convert.FromBase64String(base64Data);

            await _fileSystemService.UploadFileAsync(mappedPath, bytes, FileMode.Create, cancellationToken);

            return $@"<script>parent.az.findCmp('$ImageResizer')[0].uploadDone('{filePath}')</script>";
        }
        catch
        {
            return @"<script>parent.az.findCmp('$ImageResizer')[0].uploadError('ERROR_OCCURED_ON_SAVE')</script>";
        }
    }
}
