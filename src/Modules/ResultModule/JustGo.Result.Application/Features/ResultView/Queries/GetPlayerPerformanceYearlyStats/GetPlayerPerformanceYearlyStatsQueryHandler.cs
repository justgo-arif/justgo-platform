using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetPlayerPerformanceYearlyStats;

public class GetPlayerPerformanceYearlyStatsQueryHandler : IRequestHandler<GetPlayerPerformanceYearlyStatsQuery, Result<PlayerPerformanceYearlyStatsResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IUtilityService _utilityService;

    public GetPlayerPerformanceYearlyStatsQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _utilityService = utilityService;
    }

    public async Task<Result<PlayerPerformanceYearlyStatsResponse>> Handle(GetPlayerPerformanceYearlyStatsQuery request, CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrWhiteSpace(request.Year))
        {
            request.Year = DateTime.UtcNow.Year.ToString();
        }

        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeGuid, cancellationToken);
        var playerProfileResult = await GetPlayerProfileAsync(repo, resultEventTypeId, request, cancellationToken);

        if (!playerProfileResult.IsSuccess || playerProfileResult.Value == null)
            return Result<PlayerPerformanceYearlyStatsResponse>.Failure("No player found.", ErrorType.NotFound);

        var profile = playerProfileResult.Value;


        var response = new PlayerPerformanceYearlyStatsResponse
        {
            MemberId = profile.MemberId,
            Stats = new List<StatItemYearlyStats>
             {
                 new StatItemYearlyStats
                 {
                     Key = "yearEndRating",
                     Label = "Year End Rating",
                     Icon = "rating",
                     Value = profile.YearEndRating
                 },
                 new StatItemYearlyStats
                 {
                     Key = "highestRating",
                     Label = "Highest Rating",
                     Icon = "star",
                     Value = profile.HighestRating
                 },
                 new StatItemYearlyStats
                 {
                     Key = "tournaments",
                     Label = resultEventTypeId == 1 ? "Tournaments" : "Leagues",
                     Icon = "trophy",
                     Value = profile.TotalTournaments
                 },
                 new StatItemYearlyStats
                 {
                     Key = "matchesPlayed",
                     Label = "Matches Played",
                     Icon = "matches",
                     Value = profile.TotalMatches
                 },
                 new StatItemYearlyStats
                 {
                     Key = "matchesWon",
                     Label = "Matches Won",
                     Icon = "win",
                     Value = profile.TotalWins
                 },
                 new StatItemYearlyStats
                 {
                     Key = "matchesLoss",
                     Label = "Matches Lost",
                     Icon = "loss",
                     Value = profile.TotalLoss
                 }
             }
        };

        return response != null ? response : Result<PlayerPerformanceYearlyStatsResponse>.Failure("No player found.", ErrorType.NotFound);
    }

    private async Task<Result<PlayerInfoYearlyStats>> GetPlayerProfileAsync(IReadRepository<object> repo,int? resultEventTypeId, GetPlayerPerformanceYearlyStatsQuery request, CancellationToken cancellationToken)
    {
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
        var sql = await BuildPlayerProfileQuery(request, ownerId);
        var parameters = BuildParameters(request, resultEventTypeId, ownerId);

        var result = await repo.QueryFirstAsync<PlayerInfoYearlyStats>(sql, parameters, null, QueryType.Text, cancellationToken);

        if (result == null)
        {
            return Result<PlayerInfoYearlyStats>.Failure("Player profile not found for PlayerGuid", ErrorType.NotFound);
        }

        return result;
    }

    private async Task<string> BuildPlayerProfileQuery(GetPlayerPerformanceYearlyStatsQuery request, int ownerId)
    {
        //string categorySqlCondition = string.Empty;
        string yearSqlCondition = string.Empty;

        //if (ownerId >= 0)
        //{
        //    categorySqlCondition = " AND E.OwnerId = @OwnerId ";
        //}
 
            yearSqlCondition = " AND YEAR(E.StartDate) = @Year ";


        return $"""
            declare @PlayerUserId int = (select top 1 userid from [user] where UserSyncId =  @PlayerId)
            declare @RankingType nvarchar(100) = (select top 1 RankingType from ResultEventType where ResultEventTypeId = @ResultEventTypeId);

            ;WITH CTE_R AS (
                SELECT
                    R.UserId,
                    MAX(R.FinalRating) AS FinalRating
                FROM ResultCompetitionRankings R
                INNER JOIN ResultCompetition C ON C.CompetitionId = R.CompetitionId AND C.IsDeleted = 0 AND C.CompetitionStatusId = 2
                INNER JOIN ResultEvents E ON E.EventId = C.EventId
                WHERE R.RankingType = @RankingType
                  AND R.UserId = @PlayerUserId
                  AND E.ResultEventTypeId = @ResultEventTypeId
                  {yearSqlCondition}
                GROUP BY R.UserId
            ),
            CTE_G AS (
                SELECT TOP 1
                    R.UserId,
                    R.FinalRating
                FROM ResultCompetitionRankings R
                INNER JOIN ResultCompetition C ON C.CompetitionId = R.CompetitionId AND C.IsDeleted = 0 AND C.CompetitionStatusId = 2
                INNER JOIN ResultEvents E ON E.EventId = C.EventId
                WHERE R.RankingType = @RankingType
                  AND R.UserId = @PlayerUserId
                  AND E.ResultEventTypeId = @ResultEventTypeId
                  {yearSqlCondition}
                ORDER BY E.StartDate DESC
            ),
            MATCH_P1 AS (
                SELECT 
                    @PlayerUserId AS UserId,
                    E.EventId,
                    M.MatchId,
                    CASE WHEN M.CompetitionParticipantId = P.CompetitionParticipantId THEN 1 ELSE 0 END AS IsWin,
                    CASE WHEN M.CompetitionParticipantId2 = P.CompetitionParticipantId THEN 0 ELSE 1 END AS IsLose
                FROM ResultCompetitionRoundParticipants P
                INNER JOIN ResultCompetitionMatches M ON M.CompetitionParticipantId = P.CompetitionParticipantId AND M.IsDeleted = 0  
                INNER JOIN ResultCompetitionRounds RCR ON M.RoundId = RCR.CompetitionRoundId
                INNER JOIN ResultCompetitionInstance RCI ON RCR.InstanceId = RCI.InstanceId
                INNER JOIN ResultCompetition RC ON RC.CompetitionId = RCI.CompetitionId AND RC.IsDeleted = 0 AND RC.CompetitionStatusId = 2
                INNER JOIN ResultEvents E ON E.EventId = RC.EventId
                WHERE P.EntityId = @PlayerUserId
                  AND E.ResultEventTypeId = @ResultEventTypeId
                   {yearSqlCondition}
            ),
            MATCH_P2 AS (
                SELECT 
                    @PlayerUserId AS UserId,
                    E.EventId,
                    M.MatchId,
                    CASE WHEN M.CompetitionParticipantId = P.CompetitionParticipantId THEN 1 ELSE 0 END AS IsWin,
                    CASE WHEN M.CompetitionParticipantId2 = P.CompetitionParticipantId THEN 0 ELSE 1 END AS IsLose
                FROM ResultCompetitionRoundParticipants P
                INNER JOIN ResultCompetitionMatches M ON M.CompetitionParticipantId2 = P.CompetitionParticipantId AND M.IsDeleted = 0
                INNER JOIN ResultCompetitionRounds RCR ON M.RoundId = RCR.CompetitionRoundId
                INNER JOIN ResultCompetitionInstance RCI ON RCR.InstanceId = RCI.InstanceId
                INNER JOIN ResultCompetition RC ON RC.CompetitionId = RCI.CompetitionId AND RC.IsDeleted = 0 AND RC.CompetitionStatusId = 2
                INNER JOIN ResultEvents E ON E.EventId = RC.EventId
                WHERE P.EntityId = @PlayerUserId
                  AND E.ResultEventTypeId = @ResultEventTypeId
                    {yearSqlCondition}
            ),
            TOURNAMENTS_NO_MATCHES AS (
                SELECT 
                    @PlayerUserId AS UserId,
                    E.EventId,
                    NULL AS MatchId,
                    0 AS IsWin,
                    0 AS IsLose
                FROM ResultCompetitionRoundParticipants P
                INNER JOIN ResultCompetitionRounds RCR ON P.RoundId = RCR.CompetitionRoundId
                INNER JOIN ResultCompetitionInstance RCI ON RCR.InstanceId = RCI.InstanceId
                INNER JOIN ResultCompetition RC ON RC.CompetitionId = RCI.CompetitionId AND RC.IsDeleted = 0 AND RC.CompetitionStatusId = 2
                INNER JOIN ResultEvents E ON E.EventId = RC.EventId
                WHERE P.EntityId = @PlayerUserId
                  AND E.ResultEventTypeId = @ResultEventTypeId
                   {yearSqlCondition}
                  AND NOT EXISTS (
                    SELECT 1 
                    FROM ResultCompetitionMatches M
                    WHERE (M.CompetitionParticipantId = P.CompetitionParticipantId 
                           OR M.CompetitionParticipantId2 = P.CompetitionParticipantId)
                          AND M.IsDeleted = 0
                )
            ),
            ALL_MATCHES AS (
                SELECT * FROM MATCH_P1
                UNION ALL
                SELECT * FROM MATCH_P2
                UNION
                SELECT * FROM TOURNAMENTS_NO_MATCHES
            ),
            MATCH_SUMMARY AS (
                SELECT 
                    UserId,
                    SUM(IsWin) AS TotalWin,
                    SUM(IsLose) AS TotalLose,
                    COUNT(DISTINCT MatchId) AS TotalMatches,
                    COUNT(DISTINCT EventId) AS TotalTournaments
                FROM ALL_MATCHES
                GROUP BY UserId
            )

            SELECT 
                U.MemberId,
                R.FinalRating AS HighestRating,
                G.FinalRating AS YearEndRating,   
                M.TotalTournaments,
                M.TotalMatches,
                M.TotalWin AS TotalWins
            FROM [User] U
            LEFT JOIN CTE_R R ON R.UserId = U.UserId
            LEFT JOIN CTE_G G ON G.UserId = U.UserId
            LEFT JOIN MATCH_SUMMARY M ON M.UserId = U.UserId
            WHERE U.UserId = @PlayerUserId;
            """;
    }

    private static DynamicParameters BuildParameters(GetPlayerPerformanceYearlyStatsQuery request, int? resultEventTypeId, int ownerId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PlayerId", request.PlayerGuid);
        parameters.Add("@ResultEventTypeId", resultEventTypeId);
        parameters.Add("@Year", request.Year.Trim());

        if (ownerId >= 0)
        {
            parameters.Add("@OwnerId", ownerId);
        }

        return parameters;
    }
}
