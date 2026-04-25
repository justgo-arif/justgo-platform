using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions.GetGymnasticCompetitions;

public class GymnasticCompetitionQueryProcessor : ICompetitionQueryProcessor
{
    private readonly IReadRepository<ResultCompetitionDto> _readRepository;

    public GymnasticCompetitionQueryProcessor(IReadRepository<ResultCompetitionDto> repository)
    {
        _readRepository = repository;
    }
    public async Task<Result<ResultCompetitionDto>> QueryAsync(GetSportsCompetitionsQuery request,
        CancellationToken cancellationToken)
    {
        string sql = $"""
                           select rc.CompetitionId, 
                               rc.CompetitionName,
                               rd.[Name] as DisciplineName, 
                               count(distinct cr.CompetitionRoundId) as TotalRounds, 
                               count(distinct cp.UserId) as TotalParticipants
                           from ResultCompetition rc
                            inner join ResultDisciplines rd on rc.DisciplineId = rd.DisciplineId
                            left join ResultCompetitionRounds cr on rc.CompetitionId = cr.CompetitionId
                            left join ResultCompetitionParticipants cp on cr.CompetitionRoundId = cp.CompetitionRoundId
                                where rc.EventId = @EventId and rc.CompetitionStatusId = 2 and rc.IsDeleted = 0 {(request.FilterByDisciplineId.HasValue ? " and rc.DisciplineId = @FilterByDisciplineId " : string.Empty)}
                                    and (@Search = '' or rc.CompetitionName like '%' + @Search + '%' )
                           group by rc.CompetitionId, rc.CompetitionName, rd.[Name]
                            order by 
                                case when @SortBy = 'DisciplineName' and @OrderBy = 'ASC' then rd.[Name] end ASC,
                                case when @SortBy = 'DisciplineName' and @OrderBy = 'DESC' then rd.[Name] end DESC,
                                rc.CompetitionName
                           OFFSET (@PageNumber - 1) * @PageSize ROWS
                           FETCH NEXT @PageSize ROWS ONLY;

                           select count(*) as TotalCount
                           from ResultCompetition rc
                           inner join ResultDisciplines rd on rc.DisciplineId = rd.DisciplineId
                           where rc.EventId = @EventId and CompetitionStatusId = 2 and rc.IsDeleted = 0 {(request.FilterByDisciplineId.HasValue ? " and rc.DisciplineId = @FilterByDisciplineId " : string.Empty)}
                              and (@Search = '' or rc.CompetitionName like '%' + @Search + '%' );
                           """;

        await using var result = await _readRepository.GetMultipleQueryAsync(
            sql,
            cancellationToken,
            new
            {
                EventId = request.EventId, PageNumber = request.PageNumber,
                PageSize = request.PageSize, Search = request.Search, SortBy = request.SortBy, OrderBy = request.OrderBy,
                FilterByDisciplineId = request.FilterByDisciplineId
            },
            null,
            QueryType.Text);

        var data = await result.ReadAsync<ResultCompetitions>();
        var totalCount = await result.ReadSingleOrDefaultAsync<int>();
        
        var pagedResult = new KeysetPagedResult<ResultCompetitions>
        {
            Items = data.ToList(),
            TotalCount = totalCount
        };

        return new ResultCompetitionDto
        {
            ParticipantLabel = "players",
            AssetsLabel = "horses",
            ShowAssetsCount = false,
            Competitions = pagedResult
        };
    }
}