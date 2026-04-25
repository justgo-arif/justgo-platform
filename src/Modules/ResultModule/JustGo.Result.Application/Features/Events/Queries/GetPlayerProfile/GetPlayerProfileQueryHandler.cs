using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerProfile;

public class GetPlayerProfileQueryHandler : IRequestHandler<GetPlayerProfileQuery, Result<PlayerProfileDto>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly ILogger<GetPlayerProfileQueryHandler> _logger;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IUtilityService _utilityService;

    public GetPlayerProfileQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        ILogger<GetPlayerProfileQueryHandler> logger,
        ISystemSettingsService systemSettingsService,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _logger = logger;
        _systemSettingsService = systemSettingsService;
        _utilityService = utilityService;
    }

    public async Task<Result<PlayerProfileDto>> Handle(GetPlayerProfileQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;

        if (string.IsNullOrEmpty(request.PlayerId))
        {
            _logger.LogWarning("Player not found for PlayerId: {PlayerId}", request.PlayerId);
            return Result<PlayerProfileDto>.Failure("Player not found", ErrorType.NotFound);
        }

        var playerProfile = await GetPlayerProfileAsync(repo, request, cancellationToken);

        return playerProfile != null ? playerProfile : Result<PlayerProfileDto>.Failure("No player found.", ErrorType.NotFound);
    }

    private async Task<Result<PlayerProfileDto>> GetPlayerProfileAsync(IReadRepository<object> repo, GetPlayerProfileQuery request, CancellationToken cancellationToken)
    {
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);
        var sql = await BuildPlayerProfileQuery(request, resultEventTypeId, cancellationToken);
        var parameters = BuildParameters(request, resultEventTypeId);

        var result = await repo.QueryFirstAsync<PlayerProfileDto>(sql, parameters, null, QueryType.Text, cancellationToken);

        if (result == null)
        {
            return Result<PlayerProfileDto>.Failure("Player profile not found for PlayerId", ErrorType.NotFound);
        }

        return result;
    }

    private async Task<string> BuildPlayerProfileQuery(GetPlayerProfileQuery request, int? resultEventTypeId, CancellationToken cancellationToken)
    {
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');
        var eventTypeCondition = resultEventTypeId.HasValue ? "AND E.ResultEventTypeId = @ResultEventTypeId" : "";

        return $"""
            declare @PlayerUserId int = (select top 1 userid from [user] where UserSyncId = @PlayerId)
            ;WITH CTE_R AS (
                SELECT TOP 1
                    R.UserId,
                    R.FinalRating
                FROM ResultCompetitionRankings R
                INNER JOIN ResultCompetition C ON C.CompetitionId = R.CompetitionId
                INNER JOIN ResultEvents E ON E.EventId = C.EventId
                WHERE R.RankingType = 'Rating' AND R.UserId = @PlayerUserId
                {eventTypeCondition}
                ORDER BY E.EndDate DESC, R.CompetitionId DESC
            ),
            CTE_G AS (
                SELECT TOP 1
                    R.UserId,
                    R.FinalRating
                FROM ResultCompetitionRankings R
                INNER JOIN ResultCompetition C ON C.CompetitionId = R.CompetitionId
                INNER JOIN ResultEvents E ON E.EventId = C.EventId
                WHERE R.RankingType = 'League' AND R.UserId = @PlayerUserId
                {eventTypeCondition}
                ORDER BY E.EndDate DESC
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
                {eventTypeCondition}
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
                {eventTypeCondition}
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
                      {eventTypeCondition}
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
            ),
            CLUB_INFO AS (
                SELECT TOP 1 @PlayerUserId AS UserId, H.EntityId AS ClubDocId, H.EntityName AS ClubName
                FROM ClubMemberRoles CMR
                INNER JOIN Hierarchies H ON H.EntityId = CMR.ClubDocId
                WHERE CMR.UserId = @PlayerUserId AND CMR.IsPrimary = 1
            )

            SELECT 
                cast(U.UserSyncId as nvarchar(50)) AS PlayerId,
                U.MemberId,
                LTRIM(RTRIM(ISNULL(U.FirstName,'') + ' ' + ISNULL(U.LastName,''))) AS PlayerName,
                CASE WHEN U.ProfilePicURL <> '' THEN
                    '{baseImageUrl}/store/downloadPublic?f=' + U.ProfilePicURL + '&t=user&p=' + CAST(U.UserId AS VARCHAR)
                ELSE '' END AS PlayerImageUrl,
                U.Country,
                ISNULL(U.Gender, '') AS Gender,
                U.DOB,
                C.ClubDocId,
                C.ClubName,
                R.FinalRating AS HighestRating,
                G.FinalRating AS HighestLeagueRating,   

                M.TotalTournaments,
                M.TotalMatches,
                M.TotalWin AS TotalWins

            FROM [User] U
            LEFT JOIN CTE_R R ON R.UserId = U.UserId
            LEFT JOIN CTE_G G ON G.UserId = U.UserId
            LEFT JOIN MATCH_SUMMARY M ON M.UserId = U.UserId
            LEFT JOIN CLUB_INFO C ON C.UserId = U.UserId
            WHERE U.UserId = @PlayerUserId;
            """;
    }

    private static DynamicParameters BuildParameters(GetPlayerProfileQuery request, int? resultEventTypeId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PlayerId", request.PlayerId);

        if (resultEventTypeId.HasValue)
        {
            parameters.Add("@ResultEventTypeId", resultEventTypeId.Value);
        }

        return parameters;
    }
}
