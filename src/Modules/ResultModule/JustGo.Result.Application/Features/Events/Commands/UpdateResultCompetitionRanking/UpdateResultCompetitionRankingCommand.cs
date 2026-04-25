using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Commands.UpdateResultCompetitionRanking;

public class UpdateResultCompetitionRankingCommand : IRequest<Result<UpdateResultCompetitionRankingResponse>>
{
    public Guid? RecordGuid { get; init; }
    public int CompetitionId { get; init; }
    public string? UserGuid { get; init; }
    public decimal BeginRating { get; init; }
    public decimal FinalRating { get; init; }
    public string? RankingType { get; init; }
}
