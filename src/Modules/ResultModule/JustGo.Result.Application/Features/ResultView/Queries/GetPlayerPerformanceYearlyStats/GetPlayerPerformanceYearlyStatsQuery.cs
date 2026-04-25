using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using System.ComponentModel.DataAnnotations;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetPlayerPerformanceYearlyStats;

public class GetPlayerPerformanceYearlyStatsQuery : IRequest<Result<PlayerPerformanceYearlyStatsResponse>>
{
    [Required]
    public required string PlayerGuid { get; set; }
    [Required]
    public string ResultEventTypeGuid { get; set; }
    public string? OwnerGuid { get; set; }
    public string? Year { get; set; }

}