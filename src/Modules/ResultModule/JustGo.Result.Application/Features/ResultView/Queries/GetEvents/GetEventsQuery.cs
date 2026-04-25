using System.Text.Json.Serialization;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetEvents;

public class GetEventsQuery : PaginationParams, IRequest<Result<GenericEventListResponse>>
{
    public string? Year { get; set; }
    public string? DisciplineFilter { get; set; }
    public string? EventCategory { get; set; }
    public string? SearchTerm { get; set; }
    public string? OwnerGuid { get; set; }
    
    [JsonIgnore]
    public SportType SportType { get; set; }
}