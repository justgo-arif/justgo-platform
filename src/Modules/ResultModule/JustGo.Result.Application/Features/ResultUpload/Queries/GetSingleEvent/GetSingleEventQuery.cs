using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetSingleEvent;

public class GetSingleEventQuery : IRequest<Result<string>>
{
    public GetSingleEventQuery(int eventId)
    {
        EventId = eventId;
    }

    public int EventId { get; set; }
}