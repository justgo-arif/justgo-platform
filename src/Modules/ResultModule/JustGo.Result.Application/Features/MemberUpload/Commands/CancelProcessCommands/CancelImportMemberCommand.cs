using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.CancelProcessCommands;

public class CancelImportMemberCommand : IRequest<Result<string>>
{
    public CancelImportMemberCommand(int fileId)
    {
        FileId = fileId;
    }

    public int FileId { get; init; }
}