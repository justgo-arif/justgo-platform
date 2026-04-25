using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands;

public class UpdateMemberDataCommandHandler : IRequestHandler<UpdateMemberDataCommand, Result<string>>
{
    private readonly IResultProcessorFactory _resultProcessorFactory;

    public UpdateMemberDataCommandHandler(IResultProcessorFactory resultProcessorFactory)
    {
        _resultProcessorFactory = resultProcessorFactory;
    }

    public async Task<Result<string>> Handle(UpdateMemberDataCommand request,
        CancellationToken cancellationToken = default)
    {
        var processor = _resultProcessorFactory.GetProcessor<IUpdateMemberDataProcessor>(request.SportType, 
            ResultProcessType.UpdateMemberData);
        
        return await processor.ProcessAsync(request, cancellationToken);
    }
}