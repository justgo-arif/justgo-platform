using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventPlayers;

public class GetEventPlayersQueryHandler : IRequestHandler<GetEventPlayersQuery, Result<EventPlayersResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly ILogger<GetEventPlayersQueryHandler> _logger;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IUtilityService _utilityService;

    #region Constants
    private const string EventExistsQuery = "SELECT COUNT(1) FROM ResultEvents WHERE EventId = @EventId";

    // Fuzzy search constants
    private const int EXACT_MATCH_SCORE = 100;
    private const int PREFIX_MATCH_SCORE = 90;
    #endregion

    public GetEventPlayersQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        ILogger<GetEventPlayersQueryHandler> logger,
        ISystemSettingsService systemSettingsService,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _logger = logger;
        _systemSettingsService = systemSettingsService;
        _utilityService = utilityService;
    }

    public async Task<Result<EventPlayersResponse>> Handle(GetEventPlayersQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;

        var eventExists = await VerifyEventExistsAsync(repo, request.EventId, cancellationToken);
        if (!eventExists)
        {
            _logger.LogWarning("Event not found for EventId: {EventId}", request.EventId);
            return Result<EventPlayersResponse>.Failure("Event not found", ErrorType.NotFound);
        }

        var (players, totalCount, eventId, eventName, startDateTime) = await GetEventPlayersAsync(repo, request, cancellationToken);

        var response = new EventPlayersResponse
        {
            Players = players,
            EventId = eventId,
            EventName = eventName,
            StartDateTime = startDateTime,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return response;
    }

    private static async Task<bool> VerifyEventExistsAsync(IReadRepository<object> repo, int eventId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@EventId", eventId);

        var count = await repo.QueryFirstAsync<int>(EventExistsQuery, parameters, null, QueryType.Text, cancellationToken);
        return count > 0;
    }

    public async Task<(List<EventPlayerDto> Players, int TotalCount, int EventId, string EventName, DateTime? StartDateTime)> GetEventPlayersAsync(
        IReadRepository<object> repo,
        GetEventPlayersQuery request,
        CancellationToken cancellationToken)
    {
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);
        var sql = await BuildEventPlayersQuery(request, resultEventTypeId, cancellationToken);
        var parameters = BuildParameters(request, resultEventTypeId);

        var queryResults = await repo.GetListAsync<EventPlayerDto>(sql, parameters, null, QueryType.Text, cancellationToken);

        var eventId = queryResults.FirstOrDefault()?.EventId ?? request.EventId;
        var eventName = queryResults.FirstOrDefault()?.EventName ?? string.Empty;
        var startDateTime = queryResults.FirstOrDefault()?.StartDateTime;

        var resultList = queryResults.ToList();

        if (!resultList.Any())
        {
            return (new List<EventPlayerDto>(), 0, eventId, eventName, null);
        }

        var totalCount = resultList.FirstOrDefault()?.TotalRecords ?? 0;

        return (queryResults.ToList(), totalCount, eventId, eventName, startDateTime);
    }

    private async Task<string> BuildEventPlayersQuery(GetEventPlayersQuery request, int? resultEventTypeId, CancellationToken cancellationToken)
    {
        var whereClause = BuildWhereClause(request, resultEventTypeId);
        var orderByClause = GetOrderByClause(request);
        var hasFuzzySearch = !string.IsNullOrWhiteSpace(request.SearchTerm);
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');
        var rankingType = await GetRankingTypeAsync(resultEventTypeId, cancellationToken);

        return $"""
        ;WITH PlayerMatchData AS (
            SELECT 
                re.EventId,
                rc.CompetitionId,
                re.EventName,
                re.StartDate AS StartDateTime,
                cast(U.UserSyncId as nvarchar(50)) AS PlayerId,
                u.MemberId,
                LTRIM(RTRIM(ISNULL(u.LastName,'') + ', ' + ISNULL(u.FirstName,''))) AS PlayerName,
                ISNULL(u.LastName, '') AS LastName,
                ISNULL(u.FirstName, '') AS FirstName,
                CASE 
                    WHEN u.ProfilePicURL IS NOT NULL AND u.ProfilePicURL <> '' AND u.UserId IS NOT NULL
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u.ProfilePicURL + '&t=user&p=' + CAST(u.UserId AS VARCHAR)
                    ELSE ''
                END AS PlayerImageUrl,
                u.Address1 AS Address,
                u.Country,
                u.Gender,
                rcr.BeginRating,
                rcr.FinalRating,
                rcr.RecordGuid,
                rcm.MatchId,
                CASE 
                    WHEN rcm.WinnerCompetitionParticipantId = rcrp.CompetitionParticipantId 
                    THEN 1 ELSE 0 
                END AS IsWin,
                {(hasFuzzySearch ? GetFuzzySearchScoreExpression() : "0")} AS FuzzyScore
            FROM ResultEvents re 
            INNER JOIN ResultCompetition rc ON re.EventId = rc.EventId 
            INNER JOIN ResultCompetitionInstance rci ON rc.CompetitionId = rci.CompetitionId
            INNER JOIN ResultCompetitionRounds rcr_round ON rci.InstanceId = rcr_round.InstanceId
            INNER JOIN ResultCompetitionMatches rcm ON rcr_round.CompetitionRoundId = rcm.RoundId AND rcm.IsDeleted = 0
            INNER JOIN ResultCompetitionRoundParticipants rcrp ON (
                rcm.CompetitionParticipantId = rcrp.CompetitionParticipantId OR 
                rcm.CompetitionParticipantId2 = rcrp.CompetitionParticipantId
            )
            INNER JOIN [User] u ON rcrp.EntityId = u.UserId 
            LEFT JOIN ResultCompetitionRankings rcr 
            ON rc.CompetitionId = rcr.CompetitionId 
            AND u.UserId = rcr.UserId AND rcr.RankingType = '{rankingType}'
            WHERE re.EventId = @EventId
              AND rc.IsDeleted = 0 
              AND rc.CompetitionStatusId = 2
              AND rcrp.ParticipantType = 1
        	  {whereClause}
        ),
        PlayerStats AS (
            SELECT 
                EventId,
                CompetitionId,
                EventName,
                StartDateTime,
                PlayerId,
                MemberId,
                PlayerName,
                LastName,
                FirstName,
                PlayerImageUrl,
                Address,
                Country,
                Gender,
                BeginRating,
                FinalRating,
                RecordGuid,
                COUNT(DISTINCT MatchId) AS TotalMatches,
                SUM(IsWin) AS TotalWins,
                MAX(FuzzyScore) AS FuzzyScore
            FROM PlayerMatchData
            GROUP BY 
                EventId,CompetitionId, EventName, StartDateTime,
                PlayerId, MemberId,
                PlayerName, LastName, FirstName, PlayerImageUrl,
                Address, Country, Gender,
                BeginRating, FinalRating,RecordGuid
        ),

        AdjustmentStats AS (
            SELECT
                re.EventId,
                rc.CompetitionId,
                re.EventName,
                re.StartDate AS StartDateTime,
                cast(U.UserSyncId as nvarchar(50)) AS PlayerId,
                u.MemberId,
                LTRIM(RTRIM(ISNULL(u.FirstName,'') + ' ' + ISNULL(u.LastName,''))) AS PlayerName,
                ISNULL(u.LastName, '') AS LastName,
                ISNULL(u.FirstName, '') AS FirstName,
                CASE 
                    WHEN u.ProfilePicURL IS NOT NULL AND u.ProfilePicURL <> ''
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u.ProfilePicURL + '&t=user&p=' + CAST(u.UserId AS VARCHAR)
                    ELSE ''
                END AS PlayerImageUrl,
                u.Address1 AS Address,
                u.Country,
                u.Gender,
                rcr.BeginRating,
                rcr.FinalRating,
                rcr.RecordGuid,
                0 AS TotalMatches,
                0 AS TotalWins,
                {(hasFuzzySearch ? GetFuzzySearchScoreExpression() : "0")} AS FuzzyScore
            FROM ResultCompetitionRankings rcr
            INNER JOIN ResultCompetition rc ON rc.CompetitionId = rcr.CompetitionId AND rcr.RankingType = '{rankingType}'
            INNER JOIN ResultEvents re ON re.EventId = rc.EventId
            INNER JOIN [User] u ON u.UserId = rcr.UserId
            WHERE re.EventId = @EventId
              AND rc.IsDeleted = 0
              AND rc.CompetitionStatusId = 2
              AND NOT EXISTS (
                    SELECT 1
                    FROM ResultCompetitionMatches m
                    INNER JOIN ResultCompetitionRounds r ON m.RoundId = r.CompetitionRoundId AND m.IsDeleted = 0
                    INNER JOIN ResultCompetitionInstance i ON r.InstanceId = i.InstanceId
                    WHERE i.CompetitionId = rc.CompetitionId
              )
        )

        SELECT *,
               COUNT(*) OVER() AS TotalRecords
        FROM (
            SELECT 
                EventId,
                CompetitionId,
                EventName,
                StartDateTime,
                PlayerId,
                MemberId,
                PlayerName,
                LastName,
                FirstName,
                PlayerImageUrl,
                Address,
                Country,
                Gender,
                ISNULL(BeginRating,0) AS BeginRating,
                ISNULL(FinalRating,0) AS FinalRating,
                ISNULL(RecordGuid,'') AS RecordGuid,
                TotalMatches,
                TotalWins,
                FuzzyScore
            FROM PlayerStats

            UNION ALL

            SELECT 
                EventId,
                CompetitionId,
                EventName,
                StartDateTime,
                PlayerId,
                MemberId,
                PlayerName,
                LastName,
                FirstName,
                PlayerImageUrl,
                Address,
                Country,
                Gender,
                ISNULL(BeginRating,0),
                ISNULL(FinalRating,0),
                ISNULL(RecordGuid,''),
                TotalMatches,
                TotalWins,
                FuzzyScore
            FROM AdjustmentStats
        ) X
        ORDER BY {orderByClause}
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;
    }

    private static string GetFuzzySearchScoreExpression()
    {
        return $"""
      (CASE 
          WHEN @SearchTerm IS NULL OR @SearchTerm = '' THEN 0
          
          WHEN ISNUMERIC(@SearchTerm) = 1  AND u.MemberId = @SearchTerm THEN {EXACT_MATCH_SCORE}
          
          WHEN LOWER(LTRIM(RTRIM(ISNULL(u.LastName, '') + ' ' + ISNULL(u.FirstName, '')))) = LOWER(@SearchTerm) THEN {EXACT_MATCH_SCORE}
          
          WHEN LOWER(LTRIM(RTRIM(ISNULL(u.LastName, '') + ' ' + ISNULL(u.FirstName, '')))) LIKE LOWER(@SearchTerm + '%') THEN {PREFIX_MATCH_SCORE}
          
          ELSE 0
      END)
      """;
    }

    private static string BuildWhereClause(GetEventPlayersQuery request, int? resultEventTypeId)
    {
        var conditions = new List<string>();

        if (resultEventTypeId.HasValue)
        {
            conditions.Add("AND re.ResultEventTypeId = @ResultEventTypeId");
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            conditions.Add("""
               AND (
             	(ISNUMERIC(@SearchTerm) = 1 
             	 AND u.MemberId = @SearchTerm)
             
             	OR ISNULL(u.LastName, '') + ' ' + ISNULL(u.FirstName, '') LIKE LOWER('%' + @SearchTerm + '%')
             )
             """);
        }

        return string.Join(" ", conditions);
    }

    private static string GetOrderByClause(GetEventPlayersQuery request)
    {
        var hasFuzzySearch = !string.IsNullOrWhiteSpace(request.SearchTerm);

        if (hasFuzzySearch && string.IsNullOrWhiteSpace(request.OrderBy))
        {
            return "FuzzyScore DESC, FinalRating DESC, LastName ASC, FirstName ASC";
        }

        var orderBy = request.OrderBy?.ToLowerInvariant() switch
        {
            "finalrating" => "FinalRating",
            "playername" => "LastName, FirstName",
            "relevance" when hasFuzzySearch => "FuzzyScore",
            _ => hasFuzzySearch ? "FuzzyScore" : "FinalRating"
        };

        var direction = (orderBy == "playername" || orderBy == "FinalRating" || orderBy == "FuzzyScore") ? "DESC" : "ASC";

        if (hasFuzzySearch && !string.Equals(request.OrderBy?.ToLowerInvariant(), "relevance", StringComparison.OrdinalIgnoreCase))
        {
            if (orderBy == "FuzzyScore")
            {
                return $"FuzzyScore {direction}";
            }

            if (orderBy == "LastName, FirstName")
            {
                return $"FuzzyScore DESC, LastName {direction}, FirstName {direction}";
            }
            return $"FuzzyScore DESC, {orderBy} {direction}";
        }

        if (orderBy == "LastName, FirstName")
        {
            return $"LastName {direction}, FirstName {direction}";
        }

        return $"{orderBy} {direction}";
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

    private static DynamicParameters BuildParameters(GetEventPlayersQuery request, int? resultEventTypeId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@EventId", request.EventId);
        parameters.Add("@Offset", (request.PageNumber - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var cleanedSearchTerm = request.SearchTerm.Trim();
            parameters.Add("@SearchTerm", cleanedSearchTerm);
        }

        if (resultEventTypeId.HasValue)
        {
            parameters.Add("@ResultEventTypeId", resultEventTypeId.Value);
        }

        return parameters;
    }

}