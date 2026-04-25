using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands;

public class ConfirmUploadFileCommandHandler(IResultProcessorFactory resultProcessorFactory)
    : IRequestHandler<ConfirmUploadFileCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ConfirmUploadFileCommand request,
        CancellationToken cancellationToken = default)
    {
        var processor = resultProcessorFactory.GetProcessor<IConfirmUploadFileProcessor>(request.SportType,
            ResultProcessType.ConfirmUploadFile);
        return await processor.ProcessAsync(request, cancellationToken);
    }
}