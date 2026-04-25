using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using System.ComponentModel.DataAnnotations;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetPlayerPerformanceGlobalStats;

public class GetPlayerPerformanceGlobalStatsQuery : IRequest<Result<PlayerPerformanceGlobalStatsResponse>>
{
    [Required]
    public required string PlayerGuid { get; set; }
    [Required]
    public string ResultEventTypeGuid { get; set; }
    public string? OwnerGuid { get; set; }

}