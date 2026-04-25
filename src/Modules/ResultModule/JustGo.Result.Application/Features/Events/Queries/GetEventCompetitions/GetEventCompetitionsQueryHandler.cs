using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventCompetitions;

public class GetEventCompetitionsQueryHandler : IRequestHandler<GetEventCompetitionsQuery, Result<EventCompetitionResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly ILogger<GetEventCompetitionsQueryHandler> _logger;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IUtilityService _utilityService;

    #region Constants
    private const string EventExistsQuery = "SELECT COUNT(1) FROM ResultEvents WHERE EventId = @EventId";
    private const string GetRoundAndCompetitionIdQuery = @"
        SELECT TOP 1 rcr.CompetitionRoundId AS RoundId, rc.CompetitionId,re.EventName
        FROM ResultEvents re
        INNER JOIN ResultCompetition rc ON re.EventId = rc.EventId AND rc.IsDeleted = 0 AND rc.CompetitionStatusId = 2
        INNER JOIN ResultCompetitionInstance rci ON rc.CompetitionId = rci.CompetitionId
        INNER JOIN ResultCompetitionRounds rcr ON rci.InstanceId = rcr.InstanceId
        WHERE re.EventId = @EventId
        ORDER BY rcr.CompetitionRoundId";
    private const int UserParticipantType = 1;
    private const string MatchScoresKey = "Match Scores";
    #endregion

    public GetEventCompetitionsQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        ILogger<GetEventCompetitionsQueryHandler> logger,
        ISystemSettingsService systemSettingsService,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _logger = logger;
        _systemSettingsService = systemSettingsService;
        _utilityService = utilityService;
    }

    public async Task<Result<EventCompetitionResponse>> Handle(GetEventCompetitionsQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;

        var eventExists = await VerifyEventExistsAsync(repo, request.EventId, cancellationToken);
        if (!eventExists)
        {
            _logger.LogWarning("Event not found for EventId: {EventId}", request.EventId);
            return Result<EventCompetitionResponse>.Failure("Event not found", ErrorType.NotFound);
        }

        var (eventData, totalCount) = await GetEventDataDirectAsync(repo, request, cancellationToken);

        var response = new EventCompetitionResponse
        {
            EventData = eventData,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return eventData != null && totalCount >= 0 ? response : Result<EventCompetitionResponse>.Failure("No events found.", ErrorType.NotFound);
    }

    private static async Task<bool> VerifyEventExistsAsync(IReadRepository<object> repo, int eventId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@EventId", eventId);

        var count = await repo.QueryFirstAsync<int>(EventExistsQuery, parameters, null, QueryType.Text, cancellationToken);
        return count > 0;
    }

    private async Task<(EventData EventData, int TotalCount)> GetEventDataDirectAsync(
        IReadRepository<object> repo,
        GetEventCompetitionsQuery request,
        CancellationToken cancellationToken)
    {
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);

        var (roundId, competitionId, eventName) = await GetRoundAndCompetitionIdByEventIdAsync(repo, request.EventId, cancellationToken);

        var sql = await BuildOptimizedQueryAsync(request, resultEventTypeId, cancellationToken);
        var parameters = BuildParameters(request, resultEventTypeId);

        var matches = (await repo.GetListAsync<MatchDto>(sql, parameters, null, QueryType.Text, cancellationToken)).ToArray();

        if (!matches.Any())
        {
            return (new EventData { EventId = request.EventId, RoundId = roundId, CompetitionId = competitionId, EventName = eventName, Matches = [] }, 0);
        }

        var totalCount = matches[0].TotalRecords;
        var eventData = new EventData
        {
            EventId = request.EventId,
            RoundId = roundId,
            CompetitionId = competitionId,
            EventName = eventName,
            Matches = matches.Select(m => new CompetitionMatchDto
            {
                MatchId = m.MatchId,
                CompetitionId = m.CompetitionId,
                RoundId = m.RoundId,
                WinnerParticipantId = m.WinnerParticipantId,
                WinnerMemberId = m.WinnerMemberId,
                WinnerName = m.WinnerName,
                WinnerBeginRating = m.WinnerBeginRating,
                WinnerFinalRating = m.WinnerFinalRating,
                WinnerRatingChange = m.WinnerRatingChange,
                WinnerRatingChangeStatus = m.WinnerRatingChangeStatus,
                WinnerGender = m.WinnerGender,
                WinnerImageUrl = m.WinnerImageUrl,
                LoserParticipantId = m.LoserParticipantId,
                LoserMemberId = m.LoserMemberId,
                LoserName = m.LoserName,
                LoserBeginRating = m.LoserBeginRating,
                LoserFinalRating = m.LoserFinalRating,
                LoserRatingChange = m.LoserRatingChange,
                LoserRatingChangeStatus = m.LoserRatingChangeStatus,
                LoserGender = m.LoserGender,
                LoserImageUrl = m.LoserImageUrl,
                MatchScores = m.MatchScores,
                IsCompleted = m.IsCompleted
            }).ToList()
        };

        return (eventData, totalCount);
    }

    private static async Task<(int RoundId, int CompetitionId, string EventName)> GetRoundAndCompetitionIdByEventIdAsync(IReadRepository<object> repo, int eventId, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@EventId", eventId);

            var result = await repo.QueryFirstAsync<dynamic>(GetRoundAndCompetitionIdQuery, parameters, null, QueryType.Text, cancellationToken);
            
            if (result == null)
                return (0, 0,"");

       return ((int)result.RoundId, (int)result.CompetitionId, (string)result.EventName);
    }
    private async Task<string> BuildOptimizedQueryAsync(GetEventCompetitionsQuery request, int? resultEventTypeId, CancellationToken cancellationToken)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(request.SearchTerm);
        var orderByClause = "ORDER BY CompetitionId, MatchId";
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');

        var searchCondition = hasSearch ? GetSearchCondition() : "";
        var eventTypeCondition = resultEventTypeId.HasValue ? "AND re.ResultEventTypeId = @ResultEventTypeId" : "";
        var rankingType = await GetRankingTypeAsync(resultEventTypeId, cancellationToken);

        return $"""
            ;WITH MatchData AS (
                SELECT 
                    re.EventId,
                    re.EventName,
                    rcm.MatchId,
                    rc.CompetitionId,
                    rcr.CompetitionRoundId AS RoundId,

                    cast(U1.UserSyncId as nvarchar(50)) AS WinnerParticipantId,
                    LTRIM(RTRIM(ISNULL(u1.FirstName, '') + ' ' + ISNULL(u1.LastName, ''))) AS WinnerName,
                    ISNULL(wr.BeginRating, 0) AS WinnerBeginRating,
                    ISNULL(wr.FinalRating, 0) AS WinnerFinalRating,
                    ISNULL(mr1.RatingChange, 0) AS WinnerRatingChange,
                    ISNULL(mr1.RatingChangeStatus, 0) AS WinnerRatingChangeStatus,
                    u1.Gender AS WinnerGender,
                    u1.MemberId AS WinnerMemberId,
                    CASE 
                    WHEN u1.ProfilePicURL IS NOT NULL AND u1.ProfilePicURL != '' AND u1.UserId IS NOT NULL 
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u1.ProfilePicURL + '&t=user&p=' + CAST(u1.UserId AS VARCHAR)
                    ELSE ''
                    END AS WinnerImageUrl,

                    cast(U2.UserSyncId as nvarchar(50)) AS LoserParticipantId,
                    LTRIM(RTRIM(ISNULL(u2.FirstName, '') + ' ' + ISNULL(u2.LastName, ''))) AS LoserName,
                    ISNULL(lr.BeginRating, 0) AS LoserBeginRating,
                    ISNULL(lr.FinalRating, 0) AS LoserFinalRating,
                    ISNULL(mr2.RatingChange, 0) AS LoserRatingChange,
                    ISNULL(mr2.RatingChangeStatus, 0) AS LoserRatingChangeStatus,
                    u2.Gender AS LoserGender,
                    u2.MemberId AS LoserMemberId,
                    CASE 
                    WHEN u2.ProfilePicURL IS NOT NULL AND u2.ProfilePicURL != '' AND u2.UserId IS NOT NULL 
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u2.ProfilePicURL + '&t=user&p=' + CAST(u2.UserId AS VARCHAR)
                    ELSE ''
                    END AS LoserImageUrl,

                    rcmmd.Value AS MatchScores,
                    CASE 
                        WHEN LTRIM(RTRIM(ISNULL(u1.FirstName, '') + ' ' + ISNULL(u1.LastName, ''))) != '' 
                        THEN 1 ELSE 0 
                    END AS IsCompleted,

                    COUNT(*) OVER() AS TotalRecords

                FROM ResultEvents re
                INNER JOIN ResultCompetition rc ON re.EventId = rc.EventId AND rc.IsDeleted = 0 AND rc.CompetitionStatusId = 2
                INNER JOIN ResultCompetitionInstance rci ON rc.CompetitionId = rci.CompetitionId
                INNER JOIN ResultCompetitionRounds rcr ON rci.InstanceId = rcr.InstanceId
                INNER JOIN ResultCompetitionMatches rcm ON rcr.CompetitionRoundId = rcm.RoundId AND rcm.IsDeleted = 0
                LEFT JOIN ResultCompetitionMatchRatings mr1 on mr1.matchid = rcm.matchid and mr1.CompetitionParticipantId = rcm.WinnerCompetitionParticipantId AND mr1.IsDeleted = 0 

                LEFT JOIN ResultCompetitionMatchRatings mr2 on mr2.matchid = rcm.matchid and mr2.CompetitionParticipantId != rcm.WinnerCompetitionParticipantId AND mr2.IsDeleted = 0 

                LEFT JOIN ResultCompetitionRoundParticipants rcrp1 
                    ON rcm.CompetitionParticipantId = rcrp1.CompetitionParticipantId AND rcrp1.ParticipantType = {UserParticipantType}
                LEFT JOIN [User] u1 ON rcrp1.EntityId = u1.UserId
                LEFT JOIN ResultCompetitionRankings wr 
                    ON wr.UserId = u1.UserId 
                    AND wr.CompetitionId = rc.CompetitionId
                    AND wr.RankingType = '{rankingType}'

                LEFT JOIN ResultCompetitionRoundParticipants rcrp2 
                    ON rcm.CompetitionParticipantId2 = rcrp2.CompetitionParticipantId AND rcrp2.ParticipantType = {UserParticipantType}
                LEFT JOIN [User] u2 ON rcrp2.EntityId = u2.UserId
                LEFT JOIN ResultCompetitionRankings lr 
                    ON lr.UserId = u2.UserId 
                    AND lr.CompetitionId = rc.CompetitionId
                    AND lr.RankingType = '{rankingType}'

                LEFT JOIN ResultCompetitionMatchMetaData rcmmd ON rcm.MatchId = rcmmd.MatchId AND rcmmd.[Key] = '{MatchScoresKey}'

                WHERE re.EventId =  @EventId
                {eventTypeCondition}
                {searchCondition}
            )
            SELECT 
                EventId,
                EventName,
                MatchId,
                CompetitionId,
                RoundId,
                WinnerParticipantId,
                WinnerName,
                WinnerBeginRating,
                WinnerFinalRating,
                WinnerRatingChange,
                WinnerRatingChangeStatus,
                WinnerGender,
                WinnerImageUrl,
                LoserParticipantId,
                LoserName,
                LoserBeginRating,
                LoserFinalRating,
                LoserRatingChange,
                LoserRatingChangeStatus,
                LoserGender,
                LoserImageUrl,
                MatchScores,
                IsCompleted,
                WinnerMemberId,
                LoserMemberId,
                TotalRecords
            FROM MatchData
            {orderByClause}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
    }

    private static string GetSearchCondition()
    {
        return """
        AND (
            LOWER(LTRIM(RTRIM(ISNULL(u1.FirstName, '') + ' ' + ISNULL(u1.LastName, '')))) LIKE LOWER('%' + @SearchTerm + '%')
            OR LOWER(LTRIM(RTRIM(ISNULL(u2.FirstName, '') + ' ' + ISNULL(u2.LastName, '')))) LIKE LOWER('%' + @SearchTerm + '%')
            OR (ISNUMERIC(@SearchTerm) = 1 AND (
                u1.MemberId = @SearchTerm
                OR u2.MemberId = @SearchTerm
            ))
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

    private static DynamicParameters BuildParameters(GetEventCompetitionsQuery request, int? resultEventTypeId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@EventId", request.EventId);
        parameters.Add("@Offset", (request.PageNumber - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        var searchTerm = string.IsNullOrWhiteSpace(request.SearchTerm) ? "" : request.SearchTerm.Trim();
        parameters.Add("@SearchTerm", searchTerm);

        if (resultEventTypeId.HasValue)
            parameters.Add("@ResultEventTypeId", resultEventTypeId.Value);

        return parameters;
    }
}

