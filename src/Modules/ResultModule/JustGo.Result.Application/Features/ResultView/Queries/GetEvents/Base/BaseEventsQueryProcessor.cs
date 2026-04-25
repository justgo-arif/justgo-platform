using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using JustGo.Result.Application.Features.ResultView.Queries.GetEvents;

public abstract class BaseEventsQueryProcessor : IEventsQueryProcessor
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IUtilityService _utilityService;
    private readonly ISystemSettingsService _systemSettingsService;

    protected BaseEventsQueryProcessor(
        IReadRepositoryFactory readRepositoryFactory,
        IUtilityService utilityService,
        ISystemSettingsService systemSettingsService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _utilityService = utilityService;
        _systemSettingsService = systemSettingsService;
    }

    public async Task<Result<GenericEventListResponse>> QueryAsync(
        GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        var repository = _readRepositoryFactory.GetLazyRepository<object>().Value;
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
        var (events, totalCount) = await GetEventsWithDetailsAsync(repository, request, ownerId, cancellationToken);

        var response = new GenericEventListResponse
        {
            Events = events ?? [],
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            ShowAssets = request.SportType == SportType.Equestrian,
            ParticipantPlaceHolder = request.SportType == SportType.Equestrian ? "Horse" : "Participant"
        };

        return events != null && totalCount >= 0
            ? response
            : Result<GenericEventListResponse>.Failure("No events found.", ErrorType.NotFound);
    }

    private async Task<(List<GenericEventSummaryDto> Events, int TotalCount)> GetEventsWithDetailsAsync(
        IReadRepository<object> repository,
        GetEventsQuery request,
        int ownerId,
        CancellationToken cancellationToken)
    {
        var sql = await BuildEventListWithCountSql(request, ownerId, cancellationToken);
        var parameters = BuildParameters(request, ownerId);

        var eventData = await repository.GetListAsync<GenericEventSummaryDto>(
            sql, parameters, null, QueryType.Text, cancellationToken);
        var eventList = eventData.ToList();

        var totalCount = eventList.FirstOrDefault()?.TotalRecords ?? 0;

        return (eventList, totalCount);
    }

    private async Task<string> BuildEventListWithCountSql(
        GetEventsQuery request,
        int ownerId,
        CancellationToken cancellationToken)
    {
        var (baseWhereClause, disciplineWhereClause, searchWhereClause) = BuildWhereClause(request, ownerId);
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');

        var participantDataCte = GetParticipantDataCte();
        var participantCountColumns = GetParticipantCountColumns();
        var selectColumns = GetSelectColumns();

        return $"""
                ;WITH BaseEvents AS (
                    SELECT 
                    re.EventId,
                    re.RecordGuid ,
                    re.EventName,
                    re.StartDate AS StartDateTime,
                    re.EndDate AS EndDateTime,
                    re.EventDocId,
                    ec.CategoryName AS EventCategory,
                    re.County,
                    re.ImagePath
                FROM ResultEvents re
                LEFT JOIN ResultEventCategory ec ON re.CategoryId=ec.EventCategoryId
                WHERE 1=1
                  AND EXISTS (
                      SELECT 1 
                      FROM ResultCompetition rc 
                      WHERE rc.EventId = re.EventId
                        AND rc.IsDeleted = 0
                        AND rc.CompetitionStatusId = 2
                  )
                     {baseWhereClause}
                ),
                EventsWithDisciplines AS (
                    SELECT 
                    be.EventId,
                    be.RecordGuid,
                    be.EventName,
                    be.StartDateTime,
                    be.EndDateTime,
                    be.EventDocId,
                    be.EventCategory,
                    be.County,
                    be.ImagePath,
                    rd.Name AS DisciplineName
                FROM BaseEvents be
                INNER JOIN ResultCompetition rc ON rc.EventId = be.EventId
                    AND rc.IsDeleted = 0
                    AND rc.CompetitionStatusId = 2
                INNER JOIN ResultDisciplines rd ON rc.DisciplineId = rd.DisciplineId
                WHERE 1=1 
                        {disciplineWhereClause}
                ),
                SearchableEvents AS (
                    SELECT DISTINCT
                    EventId,
                    RecordGuid,
                    EventName,
                    StartDateTime,
                    EndDateTime,
                    EventDocId,
                    EventCategory,
                    County,
                    ImagePath
                FROM EventsWithDisciplines
                WHERE 1=1
                        {searchWhereClause}
                ),
                FilteredEvents AS (
                    SELECT 
                    EventId,
                    RecordGuid,
                    EventName,
                    StartDateTime,
                    EndDateTime,
                    EventCategory,
                    County,
                    EventDocId,
                    ImagePath,
                    COUNT(*) OVER() AS TotalRecords
                FROM SearchableEvents
                    ORDER BY StartDateTime DESC
                    OFFSET (@PageNumber - 1) * @PageSize ROWS 
                    FETCH NEXT @PageSize ROWS ONLY
                ),
                {participantDataCte},
                DisciplineList AS (
                    SELECT
                        EventId,
                        STRING_AGG(DisciplineName, ', ') AS DisciplineName
                    FROM (
                        SELECT DISTINCT EventId, DisciplineName
                        FROM ParticipantData
                    ) pd
                    GROUP BY EventId
                ),
                ParticipantCounts AS (
                    SELECT
                        EventId,
                        {participantCountColumns}
                    FROM ParticipantData
                    GROUP BY EventId
                )
                SELECT
                   fe.EventId,
                fe.EventName,
                fe.StartDateTime,
                fe.EndDateTime,
                fe.EventCategory,
                CASE 
                            WHEN fe.EventDocId > 0 and fe.ImagePath IS NOT NULL AND fe.ImagePath != 'Virtual' 
                            THEN '{baseImageUrl}/store/downloadPublic?f='+ fe.ImagePath 
                                 + '&t=repo&p='+ CAST(fe.EventDocId AS VARCHAR(20))+ '&p1=&p2=5'
                            WHEN isnull(fe.EventDocId,0) <= 0 and fe.ImagePath IS NOT NULL AND fe.ImagePath != 'Virtual' AND fe.ImagePath != '' 
                            THEN '{baseImageUrl}/store/downloadPublic?f='+ fe.ImagePath 
                                 + '&t=competitionattachment&p='+ CAST(fe.RecordGuid AS VARCHAR(50))
                            ELSE ''
                        END AS EventImageUrl,
                fe.County,
                COALESCE(dl.DisciplineName, '') AS DisciplineName,
                fe.TotalRecords,
                    {selectColumns}
                FROM FilteredEvents fe
                LEFT JOIN DisciplineList dl ON dl.EventId = fe.EventId
                LEFT JOIN ParticipantCounts pc ON pc.EventId = fe.EventId
                ORDER BY fe.StartDateTime DESC
                OPTION (OPTIMIZE FOR UNKNOWN);

                """;
    }

    protected abstract string GetParticipantDataCte();
    protected abstract string GetParticipantCountColumns();
    protected abstract string GetSelectColumns();
    protected abstract (string BaseWhereClause, string DisciplineWhereClause, string SearchWhereClause) BuildWhereClause(
        GetEventsQuery request, int ownerId);
    protected abstract DynamicParameters BuildParameters(GetEventsQuery request, int ownerId);
}