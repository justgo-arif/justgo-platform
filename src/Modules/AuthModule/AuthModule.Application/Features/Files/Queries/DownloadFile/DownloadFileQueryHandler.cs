using AuthModule.Application.DTOs.Stores;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.AspNetCore.StaticFiles;

namespace AuthModule.Application.Features.Files.Queries.DownloadFile;

public class DownloadFileQueryHandler : IRequestHandler<DownloadFileQuery, DownloadFileResultDto>
{
    private readonly IAzureBlobFileService _fileSystemService;
    private readonly ICustomError _error;
    private readonly LazyService<IReadRepository<object>> _readRepository;

    public DownloadFileQueryHandler(
        IAzureBlobFileService fileSystemService,
        ICustomError error,
        LazyService<IReadRepository<object>> readRepository)
    {
        _fileSystemService = fileSystemService;
        _error = error;
        _readRepository = readRepository;
    }

    public async Task<DownloadFileResultDto> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        var path = await ResolveDownloadPathAsync(request, cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            return new DownloadFileResultDto
            {
                Success = false,
                ErrorMessage = "File path could not be resolved"
            };
        }

        var mappedPath = await _fileSystemService.MapPath(path);

        if (!await _fileSystemService.Exists(mappedPath, cancellationToken))
        {
            return new DownloadFileResultDto
            {
                Success = false,
                ErrorMessage = "File not found"
            };
        }

        using var stream = await _fileSystemService.DownloadFileAsync(mappedPath, cancellationToken);
        if (stream == null)
        {
            return new DownloadFileResultDto
            {
                Success = false,
                ErrorMessage = "File not found"
            };
        }
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var fileBytes = ms.ToArray();

        var fileName = request.F;
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new DownloadFileResultDto
        {
            Success = true,
            FileBytes = fileBytes,
            FileName = fileName,
            ContentType = contentType
        };
    }

    private async Task<string> ResolveDownloadPathAsync(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        var t = request.T?.ToLowerInvariant();
        switch (t)
        {
            case "user":
                return $"User/{request.P}/{request.F}";
            case "repo":
                return $"Repository/{request.P2}/{request.P}/{request.F}";
            case "event":
            case "repoattach":
                return $"Repository/{request.P2}/{request.P}/{request.P1}/{request.F}";
            case "eventcategory":
                return $"media/images/events/{request.F}";
            case "license":
                return $"media/images/licenses/{request.F}";
            case "login":
                return $"media/images/login/{request.F}";
            case "organizationlogo":
                return $"media/images/organization/logo/{request.F}";
            case "organizationloginbg":
                return $"media/images/organization/LoginBg/{request.F}";
            case "organizationeventheroimage":
                return $"media/images/organization/EventHeroImage/{request.F}";
            case "organizationeventdefaultimage":
                return $"media/images/organization/EventDefaultImage/{request.F}";
            case "organizationshopdefaultimage":
                return $"media/images/organization/ShopDefaultImage/{request.F}";
            case "organizationshopheroimage":
                return $"media/images/organization/ShopHeroImage/{request.F}";
            case "organizationheroimage":
                return $"media/images/organization/HeroImage/{request.F}";
            case "resourcewebsite":
                return $"media/images/resource/website/{request.F}";
            case "mailattachment":
                var parts = request.F.Split('/');
                return $"media/images/mailattachment/{parts[0]}/{parts[1]}";
            case "eula":
                return $"media/eula/{request.F}";
            case "fieldmanagementattach":
                return $"fieldmanagementattachment/{request.P1}/{request.F}";
            case "justgobookingattachment":
                return $"justgobookingattachment/{request.P1}/{request.P}/{request.F}";
            case "competitionattachment":
                return $"competitionattachment/{request.P}/{request.F}";
            case "processingscheme":
                return $"ProcessingScheme/{request.P}/{request.F}";
            case "email":
            case "fm_content":
                return $"FroalaAttachments/{request.F}";
            case "correspondentattachments":
                return $"CorrespondentAttachments/{request.P}/{request.P1}/{request.F}";
            case "emailandcommunicationattachments":
                return $"EmailAndCommunicationAttachments/{request.P}/{request.P1}/{request.F}";
            case "jg_2025_dw_pkpass":
                return await ResolveAppleWalletFileDownloadPathAsync(request.F, cancellationToken);
            case "emailandcommunicationtemplateattachments":
                return $"EmailAndCommunicationTemplateAttachments/{request.P}/{request.F}";
            case "reportfiles":
                return $"reportfiles/{request.F}";
            case "reportattachment":
                return $"reportfiles/{request.P}/{request.F}";
            case "finance":
                return $"financeattachments/{request.F}";
            case "resultattachment":
                return $"result_attachments/{request.F}";
            case "custom":
                return request.F;
            case "wallettemplate":
            case "wallettemplatelogo":
            case "wallettemplatehero":
                return $"media/images/Digital Wallet/Template/{request.P}/{request.F}";
        }
        return string.Empty;
    }

    private async Task<string> ResolveAppleWalletFileDownloadPathAsync(string appleWalletGuid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(appleWalletGuid))
            throw new ArgumentException("Apple Wallet GUID cannot be null or empty.", nameof(appleWalletGuid));

        string sql = @"SELECT REPLACE(StoreLocation, '\', '/') as StoreLocation FROM AppleWalletInfo WHERE AppleWalletGuid = @appleWalletGuid";
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@appleWalletGuid", appleWalletGuid);

        var result = await _readRepository.Value.GetSingleAsync(sql, cancellationToken, queryParameters, null, "text");
        if (result == null)
            throw new InvalidOperationException($"No path found for Apple Wallet GUID: {appleWalletGuid}");

        var storeLocation = (result as dynamic).StoreLocation as string;
        if (string.IsNullOrEmpty(storeLocation))
            throw new InvalidOperationException($"No path found for Apple Wallet GUID: {appleWalletGuid}");

        return storeLocation;
    }
}