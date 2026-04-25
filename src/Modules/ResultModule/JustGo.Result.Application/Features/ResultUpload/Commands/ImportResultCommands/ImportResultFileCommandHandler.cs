using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;

public class ImportResultFileCommandHandler(IResultProcessorFactory resultFactory)
    : IRequestHandler<ImportResultFileCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ImportResultFileCommand request,
        CancellationToken cancellationToken = default)
    {
        var processor = resultFactory.GetProcessor<IResultProcessor>(request.SportType, 
            ResultProcessType.ProcessResult);
        return await processor.ProcessAsync(request, cancellationToken);
    }
}