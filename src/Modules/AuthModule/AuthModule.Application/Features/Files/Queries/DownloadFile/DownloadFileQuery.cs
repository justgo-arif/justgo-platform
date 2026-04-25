using AuthModule.Application.DTOs.Stores;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Queries.DownloadFile;

public class DownloadFileQuery : IRequest<DownloadFileResultDto>
{
    public string F { get; set; }
    public string T { get; set; }
    public string P { get; set; }
    public string P1 { get; set; }
    public string P2 { get; set; }
    public string P3 { get; set; }
}
