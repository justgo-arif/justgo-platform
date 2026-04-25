using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventList;

public sealed class GetEventListQueryHandler : IRequestHandler<GetEventListQuery, Result<EventListResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IUtilityService _utilityService;
    private readonly ISystemSettingsService _systemSettingsService;
    public GetEventListQueryHandler(IReadRepositoryFactory readRepositoryFactory, IUtilityService utilityService, ISystemSettingsService systemSettingsService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _utilityService = utilityService;
        _systemSettingsService = systemSettingsService;
    }

    public async Task<Result<EventListResponse>> Handle(GetEventListQuery request, CancellationToken cancellationToken = default)
    {
        var repository = _readRepositoryFactory.GetLazyRepository<object>().Value;

        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);

        var (events, totalCount) = await GetEventsWithDetailsAsync(repository, request, ownerId, cancellationToken);

        var response = new EventListResponse
        {
            Events = events ?? new List<EventSummaryDto>(),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return events != null && totalCount >= 0 ? response : Result<EventListResponse>.Failure("No events found.", ErrorType.NotFound);
    }

    private async Task<(List<EventSummaryDto> Events, int TotalCount)> GetEventsWithDetailsAsync(
        IReadRepository<object> repository,
        GetEventListQuery request,
        int ownerId,
        CancellationToken cancellationToken)
    {
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);
        var sql = await BuildEventListWithCountSql(request, ownerId, resultEventTypeId, cancellationToken);
        var parameters = BuildParameters(request, ownerId, resultEventTypeId);

        var eventData = await repository.GetListAsync<EventSummaryDto>(sql, parameters, null, QueryType.Text, cancellationToken);
        var eventList = eventData.ToList();

        var totalCount = eventList.FirstOrDefault()?.TotalRecords ?? 0;

        return (eventList, totalCount);
    }

    private async Task<string> BuildEventListWithCountSql(GetEventListQuery request, int ownerId, int? resultEventTypeId, CancellationToken cancellationToken)
    {
        var whereClause = BuildWhereClause(request, ownerId, resultEventTypeId);
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');

        return $"""
        ;WITH EventsWithCompetitions AS (
             SELECT DISTINCT
            re.EventId,
            re.EventName,
            re.Reference AS ReferenceId,
            re.StartDate AS StartDateTime,
            re.EndDate AS EndDateTime,
            ec.CategoryName AS EventCategory,
            re.County,
            re.EventDocId,
            rc.CompetitionId,
            --re.ImagePath AS EventImageUrl
            CASE 
                WHEN re.EventDocId > 0 and re.ImagePath IS NOT NULL AND re.ImagePath != 'Virtual' 
                THEN '{baseImageUrl}/store/downloadPublic?f='+ re.ImagePath 
                     + '&t=repo&p='+ CAST(re.EventDocId AS VARCHAR(20))+ '&p1=&p2=5'
                WHEN isnull(re.EventDocId,0) <= 0 and re.ImagePath IS NOT NULL AND re.ImagePath != 'Virtual' AND re.ImagePath != '' 
                THEN '{baseImageUrl}/store/downloadPublic?f='+ re.ImagePath 
                     + '&t=competitionattachment&p='+ CAST(re.RecordGuid AS VARCHAR(50))
                ELSE ''
            END AS EventImageUrl
            FROM ResultEvents re
            LEFT JOIN ResultEventCategory ec ON re.CategoryId=ec.EventCategoryId
            INNER JOIN ResultCompetition rc ON rc.EventId = re.EventId 
                AND rc.IsDeleted = 0 
                AND rc.CompetitionStatusId = 2
            WHERE 1=1
            {whereClause}
        ),

        FilteredEvents AS (
            SELECT 
                EventId,
                EventName,
                ReferenceId,
                StartDateTime,
                EndDateTime,
                EventCategory,
                County,
                CompetitionId,
                EventImageUrl,
                COUNT(*) OVER() AS TotalRecords
            FROM EventsWithCompetitions
            ORDER BY EndDateTime desc, CompetitionId desc
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        ),
        ALL_EVENT_MATCHES AS (
            SELECT 
                rc.EventId,
                m.MatchId,
                p.EntityId AS PlayerId
            FROM FilteredEvents fe
            INNER JOIN ResultCompetition rc ON rc.EventId = fe.EventId AND rc.IsDeleted = 0  AND rc.CompetitionStatusId = 2
            INNER JOIN ResultCompetitionInstance rci ON rci.CompetitionId = rc.CompetitionId
            INNER JOIN ResultCompetitionRounds rcr ON rcr.InstanceId = rci.InstanceId
            INNER JOIN ResultCompetitionMatches m ON m.RoundId = rcr.CompetitionRoundId AND m.IsDeleted = 0
            CROSS APPLY (
                SELECT m.CompetitionParticipantId
                UNION ALL
                SELECT m.CompetitionParticipantId2
            ) AS part(CompetitionParticipantId)
            INNER JOIN ResultCompetitionRoundParticipants p  ON p.CompetitionParticipantId = part.CompetitionParticipantId
        ),

        CompetitionStats AS (
            SELECT 
                EventId,
                COUNT(DISTINCT MatchId) AS TotalMatches,
                COUNT(DISTINCT PlayerId) AS TotalPlayers
            FROM ALL_EVENT_MATCHES
            GROUP BY EventId
        )

        SELECT 
             fe.EventId,
             fe.EventName,
             fe.ReferenceId,
             fe.StartDateTime,
             fe.EndDateTime,
             fe.EventCategory,
             fe.EventImageUrl,
             fe.County,
             fe.TotalRecords,
             ISNULL(cs.TotalMatches, 0) AS TotalMatches,
             ISNULL(cs.TotalPlayers, 0) AS TotalPlayers,
             NULL AS CompetitionId,
             '' AS CompetitionName,
             0 AS MatchCount,
             0 AS ParticipantCount
        FROM FilteredEvents fe
        LEFT JOIN CompetitionStats cs ON cs.EventId = fe.EventId
        ORDER BY fe.EndDateTime desc, fe.CompetitionId desc
        OPTION (OPTIMIZE FOR UNKNOWN);
        
        """;
    }

    private static string BuildWhereClause(GetEventListQuery request, int ownerId, int? resultEventTypeId)
    {
        var conditions = new List<string>();

        if (ownerId >= 0)
        {
            conditions.Add("AND re.OwnerId = @OwnerId");
        }

        if (!string.IsNullOrWhiteSpace(request.EventName))
            conditions.Add("AND LOWER(LTRIM(RTRIM(re.EventName))) LIKE LOWER('%' + LTRIM(RTRIM(@EventName)) + '%')");

        if (!string.IsNullOrWhiteSpace(request.Year))
            conditions.Add("AND YEAR(re.StartDate) IN (SELECT value FROM STRING_SPLIT(@Year, ','))");

        if (!string.IsNullOrWhiteSpace(request.EventCategory))
            conditions.Add("AND ec.CategoryName IN (SELECT value FROM STRING_SPLIT(@EventCategory, ','))");

        if (resultEventTypeId.HasValue)
            conditions.Add("AND re.ResultEventTypeId = @ResultEventTypeId");

        return string.Join(" ", conditions);
    }

    private static DynamicParameters BuildParameters(GetEventListQuery request, int ownerId, int? resultEventTypeId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@Offset", (request.PageNumber - 1) * request.PageSize);
        parameters.Add("@PageSize", request.PageSize);

        if (ownerId >= 0)
        {
            parameters.Add("@OwnerId", ownerId);
        }

        if (!string.IsNullOrWhiteSpace(request.EventName))
        {
            var cleanedEventName = request.EventName.Trim();
            parameters.Add("@EventName", cleanedEventName);
        }

        if (!string.IsNullOrWhiteSpace(request.Year))
            parameters.Add("@Year", request.Year.Trim());

        if (!string.IsNullOrWhiteSpace(request.EventCategory))
            parameters.Add("@EventCategory", request.EventCategory.Trim());

        if (resultEventTypeId.HasValue)
            parameters.Add("@ResultEventTypeId", resultEventTypeId.Value);

        return parameters;
    }
}