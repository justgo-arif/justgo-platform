using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventType
{
    public class GetEventTypeQuery : IRequest<List<EventTypeResponse>>
    {
        public bool IsProfile { get; set; } = false;
    }
}