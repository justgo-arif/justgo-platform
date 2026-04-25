using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DownloadMemberDataCommands
{
    public class DownloadMemberDataCommand(int fileId): IRequest<Result<string>>
    {
        public int FileId { get; set; } = fileId;
    }
}
