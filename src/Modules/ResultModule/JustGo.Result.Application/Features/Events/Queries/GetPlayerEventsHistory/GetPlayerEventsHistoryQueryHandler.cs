using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerEventsHistory;

public class GetPlayerEventsHistoryQueryHandler : IRequestHandler<GetPlayerEventsHistoryQuery, Result<PlayerEventsHistoryResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly ILogger<GetPlayerEventsHistoryQueryHandler> _logger;
    private readonly IUtilityService _utilityService;

    public GetPlayerEventsHistoryQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        ILogger<GetPlayerEventsHistoryQueryHandler> logger,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _logger = logger;
        _utilityService = utilityService;
    }

    public async Task<Result<PlayerEventsHistoryResponse>> Handle(GetPlayerEventsHistoryQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;

        if (string.IsNullOrEmpty(request.PlayerId))
        {
            _logger.LogWarning("Invalid PlayerId: {PlayerId}", request.PlayerId);
            return Result<PlayerEventsHistoryResponse>.Failure("Invalid player ID", ErrorType.NotFound);
        }

        // Get player events history
        var (events, totalCount) = await GetPlayerEventsHistoryAsync(repo, request, cancellationToken);

        var response = new PlayerEventsHistoryResponse
        {
            Events = events,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };

        return events != null && totalCount >= 0 ? response : Result<PlayerEventsHistoryResponse>.Failure("No event history found.", ErrorType.NotFound);
    }

    private async Task<(List<PlayerEventHistoryDto> Events, int TotalCount)> GetPlayerEventsHistoryAsync(
        IReadRepository<object> repo,
        GetPlayerEventsHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);
        var sql = await BuildPlayerEventsHistoryQuery(request, resultEventTypeId, ownerId, cancellationToken);
        var parameters = BuildParameters(request, resultEventTypeId, ownerId);

        var events = await repo.GetListAsync<PlayerEventHistoryDto>(sql, parameters, null, QueryType.Text, cancellationToken);

        var totalCount = events.FirstOrDefault()?.TotalRecords ?? 0;

        return (events.ToList(), totalCount);
    }

    private async Task<string> BuildPlayerEventsHistoryQuery(GetPlayerEventsHistoryQuery request, int? resultEventTypeId, int? ownerId, CancellationToken cancellationToken)
    {
        var eventTypeCondition = resultEventTypeId.HasValue ? "AND re.ResultEventTypeId = @ResultEventTypeId" : "";
        var rankingType = await GetRankingTypeAsync(resultEventTypeId, cancellationToken);

        //string categorySqlCondition = string.Empty;
        string yearSqlCondition = string.Empty;

        //if (ownerId >= 0)
        //{
        //    categorySqlCondition = " AND re.OwnerId = @OwnerId ";
        //}
        if (!string.IsNullOrWhiteSpace(request.Year))
        {
            yearSqlCondition = " AND YEAR(re.StartDate) = @Year ";
        }

        return $"""
            declare @PlayerUserId int = (select top 1 userid from [user] where UserSyncId = @PlayerId)
            ;WITH PlayerMatches AS (
                SELECT rcm.MatchId
                FROM ResultCompetitionMatches rcm
                INNER JOIN ResultCompetitionRoundParticipants rcrp  
                    ON rcm.CompetitionParticipantId = rcrp.CompetitionParticipantId AND rcm.IsDeleted = 0
                WHERE rcrp.ParticipantType = 1 AND rcrp.EntityId = @PlayerUserId

                UNION

                SELECT rcm.MatchId
                FROM ResultCompetitionMatches rcm
                INNER JOIN ResultCompetitionRoundParticipants rcrp  
                    ON rcm.CompetitionParticipantId2 = rcrp.CompetitionParticipantId AND rcm.IsDeleted = 0
                WHERE rcrp.ParticipantType = 1 AND rcrp.EntityId = @PlayerUserId
            ),

            BaseMatches AS (
                SELECT 
                    rcm.MatchId,
                    rc.CompetitionId,
                    re.EventName,
                    re.EventId,
                    re.StartDate,
                    re.EndDate,
                    rcm.CompetitionParticipantId AS WinnerParticipantId,
                    rcm.CompetitionParticipantId2 AS LoserParticipantId,
                    rcm.WinnerCompetitionParticipantId,
                    ec.CategoryName AS EventCategory,
                    re.County
                FROM ResultEvents re
                LEFT JOIN ResultEventCategory ec ON re.CategoryId=ec.EventCategoryId
                INNER JOIN ResultCompetition rc  ON re.EventId = rc.EventId AND rc.IsDeleted = 0 AND rc.CompetitionStatusId = 2
                INNER JOIN ResultCompetitionInstance rci ON rc.CompetitionId = rci.CompetitionId
                INNER JOIN ResultCompetitionRounds rcr ON rci.InstanceId = rcr.InstanceId
                INNER JOIN ResultCompetitionMatches rcm  ON rcr.CompetitionRoundId = rcm.RoundId AND rcm.IsDeleted = 0
                INNER JOIN PlayerMatches pm  ON rcm.MatchId = pm.MatchId
                WHERE 1=1
                {eventTypeCondition}
                {yearSqlCondition}
            ),
            MATCH_SUMMARY AS (
                SELECT
                    bm.EventId,
                    bm.EventName,
                    bm.EventCategory,
                    bm.County,
                    MIN(bm.StartDate) AS StartDate,
                    MAX(bm.EndDate) AS EndDate,
                    COUNT(*) AS TotalMatches,
                    SUM(
                        CASE 
                            WHEN bm.WinnerCompetitionParticipantId = bm.WinnerParticipantId 
                                 AND rcrp1.EntityId = @PlayerUserId THEN 1
                            WHEN bm.WinnerCompetitionParticipantId = bm.LoserParticipantId 
                                 AND rcrp2.EntityId = @PlayerUserId THEN 1
                            ELSE 0
                        END
                    ) AS TotalWin
                FROM BaseMatches bm
                LEFT JOIN ResultCompetitionRoundParticipants rcrp1 ON bm.WinnerParticipantId = rcrp1.CompetitionParticipantId
                LEFT JOIN ResultCompetitionRoundParticipants rcrp2 ON bm.LoserParticipantId = rcrp2.CompetitionParticipantId
                WHERE (rcrp1.EntityId = @PlayerUserId OR rcrp2.EntityId = @PlayerUserId)
                GROUP BY bm.EventId, bm.EventName, bm.EventCategory, bm.County
            ),
            CTE_R AS (
                SELECT 
                    RR.UserId,
                    RR.CompetitionId,
                    RR.FinalRating,
                    RR.BeginRating,
                    ROW_NUMBER() OVER (
                        PARTITION BY RR.UserId, B.EventId 
                        ORDER BY B.EndDate DESC
                    ) AS RN,
                    B.EventId
                FROM BaseMatches B
                INNER JOIN ResultCompetitionRankings RR  ON RR.CompetitionId = B.CompetitionId AND RR.RankingType = '{rankingType}'
                WHERE RR.UserId = @PlayerUserId
            ),

            CTE_DATA AS (
                SELECT 
                    R.UserId,
                    R.CompetitionId,
                    R.FinalRating,
                    R.BeginRating,
                    R.EventId
                FROM CTE_R R
                WHERE R.RN = 1
            )
            SELECT
                MS.EventId,
                MS.EventName,
                MS.EventCategory,
                MS.County,
                MS.StartDate AS StartDateTime,
                MS.EndDate AS EndDateTime,
                MS.TotalMatches,
                MS.TotalWin AS TotalWins,
                ISNULL(CD.FinalRating, 0) AS FinalRating,
                ISNULL(CD.BeginRating, 0) AS BeginRating,
                COUNT(*) OVER() AS TotalRecords
            FROM MATCH_SUMMARY MS
            INNER JOIN CTE_DATA CD ON MS.EventId = CD.EventId
            WHERE (@SearchTerm IS NULL OR LOWER(MS.EventName) LIKE LOWER('%' + @SearchTerm + '%'))
            ORDER BY MS.EndDate desc,CD.CompetitionId desc
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            OPTION (OPTIMIZE FOR UNKNOWN);
            """;
    }

    private async Task<string> GetRankingTypeAsync(int? resultEventTypeId, CancellationToken cancellationToken)
    {
        if (!resultEventTypeId.HasValue)
            return "Rating";

        var rankingType = await _readRepositoryFactory.GetLazyRepository<object>().Value.QueryFirstAsync<string>(
          "SELECT RankingType FROM ResultEventType WHERE ResultEventTypeId = @ResultEventTypeId",
          new { ResultEventTypeId = resultEventTypeId },
          null,
          QueryType.Text,
          cancellationToken);

        return string.IsNullOrWhiteSpace(rankingType) ? "Rating" : rankingType;
    }

    private static DynamicParameters BuildParameters(GetPlayerEventsHistoryQuery request, int? resultEventTypeId, int? ownerId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PlayerId", request.PlayerId);
        parameters.Add("@Offset", (request.PageNumber - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);
        parameters.Add("@SearchTerm", string.IsNullOrWhiteSpace(request.SearchTerm) ? null : request.SearchTerm.Trim());

        if (resultEventTypeId.HasValue)
        {
            parameters.Add("@ResultEventTypeId", resultEventTypeId.Value);
        }
        if (ownerId.HasValue)
        {
            parameters.Add("@OwnerId", ownerId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Year))
        {
            parameters.Add("@Year", request.Year);
        }

        return parameters;
    }
}