using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetPlayerPerformanceGlobalStats;

public class GetPlayerPerformanceGlobalStatsQueryHandler : IRequestHandler<GetPlayerPerformanceGlobalStatsQuery, Result<PlayerPerformanceGlobalStatsResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IUtilityService _utilityService;

    public GetPlayerPerformanceGlobalStatsQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _utilityService = utilityService;
    }

    public async Task<Result<PlayerPerformanceGlobalStatsResponse>> Handle(GetPlayerPerformanceGlobalStatsQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;

        var playerProfileResult = await GetPlayerProfileAsync(repo, request, cancellationToken);

        if (!playerProfileResult.IsSuccess || playerProfileResult.Value == null)
            return Result<PlayerPerformanceGlobalStatsResponse>.Failure("No player found.", ErrorType.NotFound);

        var profile = playerProfileResult.Value;


        var response = new PlayerPerformanceGlobalStatsResponse
        {
            MemberId = profile.MemberId,
            Stats = new List<StatItem>
             {
                 new StatItem
                 {
                     Label = "Current Rating",
                     Icon = "rating",
                     Value = profile.CurrentRating
                 },
                 new StatItem
                 {
                     Label = "Highest Rating",
                     Icon = "star",
                     Value = profile.HighestRating
                 },
                 new StatItem
                 {
                     Label = "Win Loss %",
                     Icon = "percentage",
                     Value = profile.WinPercentage
                 }
             }
        };


        return response != null ? response : Result<PlayerPerformanceGlobalStatsResponse>.Failure("No player found.", ErrorType.NotFound);
    }

    private async Task<Result<PlayerInfo>> GetPlayerProfileAsync(IReadRepository<object> repo, GetPlayerPerformanceGlobalStatsQuery request, CancellationToken cancellationToken)
    {
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeGuid, cancellationToken);
        var sql = await BuildPlayerProfileQuery(ownerId);
        var parameters = BuildParameters(request, resultEventTypeId, ownerId);

        var result = await repo.QueryFirstAsync<PlayerInfo>(sql, parameters, null, QueryType.Text, cancellationToken);

        if (result == null)
        {
            return Result<PlayerInfo>.Failure("Player profile not found for PlayerGuid", ErrorType.NotFound);
        }

        return result;
    }

    private async Task<string> BuildPlayerProfileQuery(int ownerId)
    {
        //string categorySqlCondition = string.Empty;

        //if (ownerId >= 0)
        //{
        //    categorySqlCondition = " AND E.OwnerId = @OwnerId ";
        //}


        return $"""
            declare @PlayerUserId int = (select top 1 userid from [user] where UserSyncId =  @PlayerId)
            declare @RankingType nvarchar(100) = (select top 1 RankingType from ResultEventType where ResultEventTypeId = @ResultEventTypeId);

            ;WITH CTE_R AS (
                SELECT
                    R.UserId,
                    MAX(R.FinalRating) AS FinalRating
                FROM ResultCompetitionRankings R
                INNER JOIN ResultCompetition C ON C.CompetitionId = R.CompetitionId  AND  C.IsDeleted = 0 AND  C.CompetitionStatusId = 2
                INNER JOIN ResultEvents E ON E.EventId = C.EventId
                WHERE R.RankingType = @RankingType
                  AND R.UserId = @PlayerUserId
                  AND E.ResultEventTypeId = @ResultEventTypeId  
                GROUP BY R.UserId
            ),
            CTE_CurrentRating AS (
                SELECT TOP 1
                    R.UserId,
                    R.FinalRating
                FROM ResultCompetitionRankings R
                INNER JOIN ResultCompetition C ON C.CompetitionId = R.CompetitionId AND  C.IsDeleted = 0 AND  C.CompetitionStatusId = 2
                INNER JOIN ResultEvents E ON E.EventId = C.EventId
                WHERE R.RankingType = @RankingType
                  AND R.UserId = @PlayerUserId
                  AND E.ResultEventTypeId = @ResultEventTypeId 
                ORDER BY E.StartDate DESC, R.CompetitionId DESC
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
            ),
            ALL_MATCHES AS (
                SELECT * FROM MATCH_P1
                UNION ALL
                SELECT * FROM MATCH_P2
            ),
            MATCH_SUMMARY AS (
                SELECT 
                    UserId,
                    SUM(IsWin) AS TotalWin,
                    SUM(IsLose) AS TotalLose,
                    COUNT(DISTINCT MatchId) AS TotalMatches
                FROM ALL_MATCHES
                GROUP BY UserId
            )

            SELECT 
                U.MemberId,
                R.FinalRating AS HighestRating,
                CR.FinalRating AS CurrentRating,
                M.TotalMatches,
                M.TotalWin AS TotalWins
            FROM [User] U
            LEFT JOIN CTE_R R ON R.UserId = U.UserId
            LEFT JOIN CTE_CurrentRating CR ON CR.UserId = U.UserId
            LEFT JOIN MATCH_SUMMARY M ON M.UserId = U.UserId
            WHERE U.UserId = @PlayerUserId;
            """;
    }

    private static DynamicParameters BuildParameters(GetPlayerPerformanceGlobalStatsQuery request, int? resultEventTypeId, int ownerId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PlayerId", request.PlayerGuid);
        parameters.Add("@ResultEventTypeId", resultEventTypeId);

        if (ownerId >= 0)
        {
            parameters.Add("@OwnerId", ownerId);
        }

        return parameters;
    }
}
