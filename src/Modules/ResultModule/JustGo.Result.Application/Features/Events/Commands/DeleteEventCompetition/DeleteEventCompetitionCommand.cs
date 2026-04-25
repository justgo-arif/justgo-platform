using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Commands.DeleteEventCompetition;

public class DeleteEventCompetitionCommand : IRequest<Result<DeleteEventCompetitionResponse>>
{
    [Required]
    public required int MatchId { get; set; }
}