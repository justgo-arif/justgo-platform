using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetEvents;

public interface IEventsQueryProcessor
{
    Task<Result<GenericEventListResponse>> QueryAsync(GetEventsQuery request,
        CancellationToken cancellationToken);
}