using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventList;

public class GetEventListQuery : PaginationParams , IRequest<Result<EventListResponse>>
{
    public string? EventName { get; set; }
    public string? Year { get; set; }
    public string? EventCategory { get; set; }
    public string? OwnerGuid { get; set; }
    public string? ResultEventTypeId { get; set; } 
}