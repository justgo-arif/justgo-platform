using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetFilterMetaData;

public class GetFilterMetaDataQueryHandler : IRequestHandler<GetFilterMetaDataQuery, FilterMetadataDto>
{
    private readonly IReadRepository<FilterMetadataDto> _resultCompetitionRepository;

    public GetFilterMetaDataQueryHandler(IReadRepository<FilterMetadataDto> resultCompetitionRepository)
    {
        _resultCompetitionRepository = resultCompetitionRepository;
    }

    public async Task<FilterMetadataDto> Handle(GetFilterMetaDataQuery request, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT DISTINCT
                               RD.[Key],
                               RD.[Value]
                           FROM ResultCompetition RC
                           INNER JOIN ResultCompetitionRounds CR ON RC.CompetitionId = CR.CompetitionId
                           INNER JOIN ResultCompetitionParticipants CP ON CR.CompetitionRoundId = CP.CompetitionRoundId
                           INNER JOIN ResultCompetitionResults RCR ON CP.CompetitionParticipantId = RCR.CompetitionParticipantId
                           INNER JOIN ResultCompetitionResultData RD ON RCR.CompetitionResultId = RD.CompetitionResultId
                           WHERE RC.CompetitionId = @CompetitionId AND RC.IsDeleted = 0
                               AND (@RoundId IS NULL OR CR.CompetitionRoundId = @RoundId)
                               AND RD.[Key] IN (
                           		SELECT LTRIM(RTRIM(s.value)) AS FilterKey
                           		FROM ResultCompetition rc
                           		INNER JOIN ResultDisciplines rd ON rc.DisciplineId = rd.DisciplineId
                           		CROSS APPLY STRING_SPLIT(rd.ResultViewFilterKeys, ',') s
                           		WHERE rc.CompetitionId = @CompetitionId
                           	)
                           """;

        var data = await _resultCompetitionRepository.GetListAsync<(string Key, string Value)>(sql, new
        {
            CompetitionId = request.CompetitionId,
            RoundId = request.RoundId
        }, null, QueryType.Text, cancellationToken);
        
        var filterOptions = data
            .GroupBy(d => d.Key)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Value).Distinct().ToList());
        
        return new FilterMetadataDto
        {
            FilterOptions = filterOptions
        };
    }
}