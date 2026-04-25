using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.EmailMemberUploadStatusCommands;

public class EmailMemberUploadStatusCommand : IRequest<Result<string>>
{
    public EmailMemberUploadStatusCommand(int fileId)
    {
        FileId = fileId;
    }

    public int FileId { get; set; }
}