using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetFilterMetaData;

public class GetFilterMetaDataQuery : IRequest<FilterMetadataDto>
{
    public int CompetitionId { get; set; }
    public int? RoundId { get; set; } = null;
    
    public GetFilterMetaDataQuery(int competitionId, int? roundId)
    {
        CompetitionId = competitionId;
        RoundId = roundId;
    }
}