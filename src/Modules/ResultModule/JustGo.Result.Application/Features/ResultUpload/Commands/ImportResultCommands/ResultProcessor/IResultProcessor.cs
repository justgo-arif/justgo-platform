using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ResultProcessor;

public interface IResultProcessor
{
    Task<Result<string>> ProcessAsync(ImportResultFileCommand request, CancellationToken cancellationToken);
}
