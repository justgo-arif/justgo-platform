using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DeleteMemberUploadFileCommands;

public class DeleteMemberUploadFileCommand(int uploadedFileId) : IRequest<Result<bool>>
{
    public int UploadedFileId { get; set; } = uploadedFileId;
}