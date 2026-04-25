using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Commands.UpdateEventCompetition;

public class UpdateEventCompetitionCommand : IRequest<Result<UpdateEventCompetitionResponse>>
{
    [Required]
    public required int MatchId { get; set; }

    [Required]
    public required int EventId { get; set; }

    [Required]
    public required int CompetitionId { get; set; }

    [Required]
    public required int RoundId { get; set; }

    [Required]
    public required string Player1UserId { get; set; }

    [Required]
    public required string Player2UserId { get; set; }

    [Required]
    public required string WinnerUserId { get; set; }

    public int Player1RatingChange { get; set; } = 0;

    public  int Player2RatingChange { get; set; } = 0;

    public string? MatchScores { get; set; }
}