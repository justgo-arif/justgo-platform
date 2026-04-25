using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.UpdateFileStatusCommands;

public class UpdateFileStatusCommand : IRequest<Result<string>>
{
    public int UploadFileId { get; init; }
    public FileStatus UploadFileStatus { get; init; }

    public UpdateFileStatusCommand(int uploadFileId, FileStatus uploadFileStatus)
    {
        UploadFileStatus = uploadFileStatus;
        UploadFileId = uploadFileId;
    }
}