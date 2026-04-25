using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands;

public class UploadResultFileCommandHandler(IResultProcessorFactory resultProcessorFactory)
    : IRequestHandler<UploadResultFileCommand, Result<FileHeaderResponseDto>>
{
    public async Task<Result<FileHeaderResponseDto>> Handle(UploadResultFileCommand request,
        CancellationToken cancellationToken = default)
    {
        var processor = resultProcessorFactory.GetProcessor<IUploadResultFileProcessor>(request.SportType,
                ResultProcessType.UploadFile);
        return await processor.ProcessAsync(request, cancellationToken);
    }
}