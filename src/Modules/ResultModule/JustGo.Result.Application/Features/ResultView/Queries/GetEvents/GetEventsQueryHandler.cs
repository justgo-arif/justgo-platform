using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.DTOs.Events;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetEvents;

public class GetEventsQueryHandler(IResultProcessorFactory resultFactory)
    : IRequestHandler<GetEventsQuery, Result<GenericEventListResponse>>
{
    public async Task<Result<GenericEventListResponse>> Handle(GetEventsQuery request, CancellationToken cancellationToken = default)
    {
        var processor = resultFactory.GetProcessor<IEventsQueryProcessor>(request.SportType,
            ResultProcessType.RetrieveEvents);
        return await processor.QueryAsync(request, cancellationToken);
    }
}