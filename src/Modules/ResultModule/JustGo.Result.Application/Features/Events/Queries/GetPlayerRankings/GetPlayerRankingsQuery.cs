using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerRankings;

public class GetPlayerRankingsQuery : PaginationParams, IRequest<Result<PlayerRankingsResponse>>
{
    public string? SearchTerm { get; set; }
    public string? OwnerGuid { get; set; }
    public string? Gender { get; set; }
    public string? County { get; set; }
    public int? All { get; set; }
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? ResultEventTypeId { get; set; } 
}   