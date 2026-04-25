using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetEventDisciplines;

public class GetEventDisciplinesQuery : IRequest<Result<ICollection<EventDisciplineDto>>>
{
    public GetEventDisciplinesQuery(int eventId)
    {
        EventId = eventId;
    }

    public int EventId { get; }
}