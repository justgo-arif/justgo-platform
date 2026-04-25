using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands;

public interface IUpdateMemberDataProcessor
{
    Task<Result<string>> ProcessAsync(UpdateMemberDataCommand request,
        CancellationToken cancellationToken);
}