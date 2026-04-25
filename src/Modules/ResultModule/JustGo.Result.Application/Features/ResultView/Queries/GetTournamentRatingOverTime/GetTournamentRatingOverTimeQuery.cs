using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using System.ComponentModel.DataAnnotations;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetTournamentRatingOverTime;

public class GetTournamentRatingOverTimeQuery
    : IRequest<Result<List<TournamentRatingOverTimeResponse>>>
{
    [Required]
    public required string PlayerGuid { get; set; }

    [Required]
    public required string ResultEventTypeGuid { get; set; }

    public string? OwnerGuid { get; set; }

    public string? FromDate { get; set; }

    public string? ToDate { get; set; }
}