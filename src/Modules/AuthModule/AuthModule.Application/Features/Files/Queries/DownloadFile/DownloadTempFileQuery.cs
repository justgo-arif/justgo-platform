using AuthModule.Application.DTOs.Stores;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Queries.DownloadFile;

public class DownloadTempFileQuery : IRequest<DownloadTempFileResultDto>
{
    public string Path { get; set; }
}
