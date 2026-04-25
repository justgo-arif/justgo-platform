using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands;

public interface IConfirmUploadFileProcessor
{
    Task<Result<int>> ProcessAsync(ConfirmUploadFileCommand request,
        CancellationToken cancellationToken);
}