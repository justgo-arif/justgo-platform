using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerMatchHistory;

public class GetPlayerMatchHistoryQueryHandler : IRequestHandler<GetPlayerMatchHistoryQuery, Result<PlayerMatchHistoryResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly ILogger<GetPlayerMatchHistoryQueryHandler> _logger;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IUtilityService _utilityService;

    #region Constants
    private const string PlayerExistsQuery = "SELECT COUNT(1) FROM [User] WHERE UserSyncId = @PlayerId";
    private const string MatchScoresKey = "Match Scores";
    #endregion

    public GetPlayerMatchHistoryQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        ILogger<GetPlayerMatchHistoryQueryHandler> logger,
        ISystemSettingsService systemSettingsService,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _logger = logger;
        _systemSettingsService = systemSettingsService;
        _utilityService = utilityService;
    }

    public async Task<Result<PlayerMatchHistoryResponse>> Handle(GetPlayerMatchHistoryQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;

        var playerExists = await VerifyPlayerExistsAsync(repo, request.PlayerId, cancellationToken);
        if (!playerExists)
        {
            _logger.LogWarning("Player not found for PlayerId: {PlayerId}", request.PlayerId);
            return Result<PlayerMatchHistoryResponse>.Failure("Player not found", ErrorType.NotFound);
        }

        var playerSummary = await GetPlayerSummaryAsync(repo, request.PlayerId, cancellationToken);

        var (matches, totalCount) = await GetPlayerMatchHistoryAsync(repo, request, cancellationToken);

        var response = new PlayerMatchHistoryResponse
        {
            PlayerSummary = playerSummary,
            Matches = matches,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            EventName = matches.FirstOrDefault()?.EventName,
            StartDateTime = matches.FirstOrDefault()?.StartDateTime
        };

        return matches != null && totalCount >= 0 ? response : Result<PlayerMatchHistoryResponse>.Failure("No match history found.", ErrorType.NotFound);
    }

    private static async Task<bool> VerifyPlayerExistsAsync(IReadRepository<object> repo, string playerId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@PlayerId", playerId);

        var count = await repo.QueryFirstAsync<int>(PlayerExistsQuery, parameters, null, QueryType.Text, cancellationToken);
        return count > 0;
    }

    private static async Task<PlayerSummaryDto> GetPlayerSummaryAsync(IReadRepository<object> repo, string playerId, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT 
                cast(U.UserSyncId as nvarchar(50)) as PlayerId,
                ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') as PlayerName,
                u.Gender as PlayerGender,
                u.Country
            FROM [User] u
            LEFT JOIN ResultCompetitionRankings rcr ON u.UserId = rcr.UserId 
            WHERE u.UserSyncId = @PlayerId
            GROUP BY u.UserSyncId, u.FirstName, u.LastName, u.Gender, u.Country
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@PlayerId", playerId);

        var result = await repo.QueryFirstAsync<PlayerSummaryDto>(sql, parameters, null, QueryType.Text, cancellationToken);
        return result ?? new PlayerSummaryDto { PlayerId = playerId };
    }

    private async Task<(List<PlayerMatchHistoryDto> Matches, int TotalCount)> GetPlayerMatchHistoryAsync(
        IReadRepository<object> repo,
        GetPlayerMatchHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);
        var sql = await BuildPlayerMatchHistoryQuery(request, resultEventTypeId, cancellationToken);
        var parameters = BuildParameters(request, resultEventTypeId);

        var matches = await repo.GetListAsync<PlayerMatchHistoryDto>(sql, parameters, null, QueryType.Text, cancellationToken);

        var totalCount = matches.FirstOrDefault()?.TotalRecords ?? 0;

        return (matches.ToList(), totalCount);
    }

    private async Task<string> BuildPlayerMatchHistoryQuery(GetPlayerMatchHistoryQuery request, int? resultEventTypeId, CancellationToken cancellationToken)
    {
        var whereClause = BuildWhereClause(request);
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');
        var eventTypeCondition = resultEventTypeId.HasValue ? "AND re.ResultEventTypeId = @ResultEventTypeId" : "";
        var rankingType = await GetRankingTypeAsync(resultEventTypeId, cancellationToken);

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
                    rc.CompetitionName,
                    re.EventName,
                    re.StartDate AS StartDateTime,
                    re.EventId,
                    rcr.CompetitionRoundId AS RoundId,
                    rcr.RoundName,
                    rcr.StartDate AS MatchDate,
                    rcm.CompetitionParticipantId AS WinnerParticipantId,
                    rcm.CompetitionParticipantId2 AS LoserParticipantId,
                    rcm.WinnerCompetitionParticipantId,
                    rcmmd.Value AS MatchScores
                FROM ResultEvents re
                INNER JOIN ResultCompetition rc   ON re.EventId = rc.EventId AND rc.IsDeleted = 0 AND rc.CompetitionStatusId = 2
                INNER JOIN ResultCompetitionInstance rci   ON rc.CompetitionId = rci.CompetitionId
                INNER JOIN ResultCompetitionRounds rcr   ON rci.InstanceId = rcr.InstanceId
                INNER JOIN ResultCompetitionMatches rcm   ON rcr.CompetitionRoundId = rcm.RoundId AND rcm.IsDeleted = 0
                LEFT JOIN ResultCompetitionMatchMetaData rcmmd   ON rcm.MatchId = rcmmd.MatchId AND rcmmd.[Key] = '{MatchScoresKey}'

                INNER JOIN PlayerMatches pm   ON rcm.MatchId = pm.MatchId
                WHERE (@EventId IS NULL OR re.EventId = @EventId)
                {eventTypeCondition}
            )

            SELECT 
                bm.MatchId,
                bm.CompetitionId,
                bm.CompetitionName,
                bm.EventName,
                bm.StartDateTime,
                bm.RoundId,
                bm.RoundName,
                bm.MatchDate,
                bm.MatchScores,

                -- Winner Info
                cast(u1.UserSyncId as nvarchar(50)) AS WinnerParticipantId,
                ISNULL(u1.FirstName,'') + ' ' + ISNULL(u1.LastName,'') AS WinnerName,
                u1.Gender AS WinnerGender,
                u1.MemberId AS WinnerMemberId,
                ISNULL(r1.BeginRating,0) AS WinnerBeginRating,
                ISNULL(r1.FinalRating,0) AS WinnerFinalRating,
                ISNULL(mr1.RatingChange, 0) AS WinnerRatingChange,
                ISNULL(mr1.RatingChangeStatus, 0) AS WinnerRatingChangeStatus,
                CASE 
                    WHEN u1.ProfilePicURL IS NOT NULL AND u1.ProfilePicURL <> '' 
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u1.ProfilePicURL + '&t=user&p=' + CAST(u1.UserId AS VARCHAR)
                    ELSE ''
                END AS WinnerImageUrl,

                -- Loser Info
                cast(u2.UserSyncId as nvarchar(50)) AS LoserParticipantId,
                ISNULL(u2.FirstName,'') + ' ' + ISNULL(u2.LastName,'') AS LoserName,
                u2.Gender AS LoserGender,
                u2.MemberId AS LoserMemberId,
                ISNULL(r2.BeginRating,0) AS LoserBeginRating,
                ISNULL(r2.FinalRating,0) AS LoserFinalRating,
                ISNULL(mr2.RatingChange, 0) AS LoserRatingChange,
                ISNULL(mr2.RatingChangeStatus, 0) AS LoserRatingChangeStatus,
                CASE 
                    WHEN u2.ProfilePicURL IS NOT NULL AND u2.ProfilePicURL <> '' 
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u2.ProfilePicURL + '&t=user&p=' + CAST(u2.UserId AS VARCHAR)
                    ELSE ''
                END AS LoserImageUrl,

                CASE WHEN bm.WinnerCompetitionParticipantId IS NOT NULL THEN 1 ELSE 0 END AS IsCompleted,
                CASE WHEN w.EntityId = @PlayerUserId THEN 1 ELSE 0 END AS IsPlayerWinner,
                '' AS FuzzyScore,
                COUNT(*) OVER() AS TotalRecords  

            FROM BaseMatches bm

                LEFT JOIN ResultCompetitionMatchRatings mr1 on mr1.matchid = bm.matchid and mr1.CompetitionParticipantId = bm.WinnerParticipantId AND mr1.IsDeleted = 0 

                LEFT JOIN ResultCompetitionMatchRatings mr2 on mr2.matchid = bm.matchid and mr2.CompetitionParticipantId != bm.WinnerParticipantId AND mr2.IsDeleted = 0


            LEFT JOIN ResultCompetitionRoundParticipants w   ON bm.WinnerParticipantId = w.CompetitionParticipantId AND w.ParticipantType = 1
            LEFT JOIN [User] u1   ON w.EntityId = u1.UserId
            LEFT JOIN ResultCompetitionRankings r1  ON bm.CompetitionId = r1.CompetitionId AND w.EntityId = r1.UserId AND r1.RankingType = '{rankingType}'
            LEFT JOIN ResultCompetitionRoundParticipants l   ON bm.LoserParticipantId = l.CompetitionParticipantId AND l.ParticipantType = 1
            LEFT JOIN [User] u2   ON l.EntityId = u2.UserId
            LEFT JOIN ResultCompetitionRankings r2  ON bm.CompetitionId = r2.CompetitionId AND l.EntityId = r2.UserId AND r2.RankingType = '{rankingType}'
            {whereClause}
            ORDER BY bm.MatchDate DESC, bm.MatchId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            OPTION (OPTIMIZE FOR UNKNOWN);
            """;
    }

    private static string BuildWhereClause(GetPlayerMatchHistoryQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return string.Empty;
        }

        return """
            WHERE (
                  u1.MemberId = @SearchTerm 
                OR u2.MemberId = @SearchTerm
                OR LOWER(ISNULL(u1.FirstName,'') + ' ' + ISNULL(u1.LastName,'')) LIKE LOWER('%' + @SearchTerm + '%')
                OR LOWER(ISNULL(u2.FirstName,'') + ' ' + ISNULL(u2.LastName,'')) LIKE LOWER('%' + @SearchTerm + '%')
            )
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

    private static DynamicParameters BuildParameters(GetPlayerMatchHistoryQuery request, int? resultEventTypeId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PlayerId", request.PlayerId);
        parameters.Add("@EventId", request.EventId.HasValue ? request.EventId.Value : null);
        parameters.Add("@Offset", (request.PageNumber - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            parameters.Add("@SearchTerm", request.SearchTerm.Trim());
        }

        if (resultEventTypeId.HasValue)
        {
            parameters.Add("@ResultEventTypeId", resultEventTypeId.Value);
        }

        return parameters;
    }

}