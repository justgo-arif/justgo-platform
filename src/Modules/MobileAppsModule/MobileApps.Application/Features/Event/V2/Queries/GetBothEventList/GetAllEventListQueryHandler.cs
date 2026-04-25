using System.Threading;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetBothEventList
{
    class GetEventListQueryHandler : IRequestHandler<GetAllEventListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetEventListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetAllEventListQuery request, CancellationToken cancellationToken)
        {

            //default date rand from one month behind
            if (string.IsNullOrWhiteSpace(request.EventName)  && string.IsNullOrWhiteSpace(request.StartDate)  && string.IsNullOrWhiteSpace(request.EndDate))
            {
                // First day of the previous month in current year
                var now = DateTime.UtcNow;
                var prevMonth = now.AddMonths(-1);
                request.StartDate = new DateTime(prevMonth.Year, prevMonth.Month, 1).ToString("yyyy-MM-dd");

                // A date far in the future (covers "greater than all data")
                request.EndDate = DateTime.MaxValue.ToString("yyyy-MM-dd");
            }
            string sql = GetBothEventListQL(request);
            string sqlWhere = "";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId", request.ClubDocId);


            if (!string.IsNullOrEmpty(request.EventName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND NormEventName Like '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.EventName);
            }


            if (string.IsNullOrEmpty(request.EndDate) && !string.IsNullOrEmpty(request.StartDate))
            {
                DateTime startDate = DateTime.Parse(request.StartDate);
               
                sqlWhere = sqlWhere + @" AND (Cast(StartDateKey AS DATE) <= Cast(@EndDate AS DATE)  
AND Cast(EndDateKey AS DATE) >= Cast(@StartDate AS DATE))";
                queryParameters.Add("@StartDate", startDate);
                queryParameters.Add("@EndDate", startDate);
            }
            else if (!string.IsNullOrEmpty(request.EndDate) && !string.IsNullOrEmpty(request.StartDate))
            {
                DateTime startDate = DateTime.Parse(request.StartDate);
                DateTime endDate = DateTime.Parse(request.EndDate);

                sqlWhere = sqlWhere + @" AND (Cast(StartDateKey AS DATE) <= Cast(@EndDate AS DATE) AND Cast(EndDateKey AS DATE) >= Cast(@StartDate AS DATE))";
                queryParameters.Add("@StartDate", startDate);
                queryParameters.Add("@EndDate", endDate);
            }

            if(request.IsETicket)
            {
                sqlWhere = sqlWhere + @" AND IsETicket = 1";
            }
            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            var eventList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

            if(eventList!=null && eventList.Count>0) await GetEventLocation(eventList, cancellationToken);

            return eventList;
        }

        private static string GetBothEventListQL(GetAllEventListQuery request)
        {
            int nextId = request.NextId <= 0 ? 0 : request.NextId + 1;
            int dataSize = (request.NextId + request.DataSize);
            return $@"-- eTicket: Use an EXISTS filter for better join perf
            ;WITH EticketCTE AS (
                SELECT cb.Coursedocid
                FROM CourseBooking_Default cb
                WHERE EXISTS (
                    SELECT 1 FROM Products_Default pd
                    WHERE pd.DocId = cb.Productdocid
                      AND pd.Wallettemplateid > 0
                )
                GROUP BY cb.Coursedocid
            ),
            BaseEvents AS (
                SELECT
                    ed.DocId,
                    ed.Location,
                    ed.Postcode,
                    ed.EventName,
                    ed.Address1,
                    ed.Country,
                    ed.EventLocation,
                    ed.County,
                    ed.Town,
                    ed.Timezone,
                    ed.RepositoryId,
                    COALESCE(ed.Isrecurring, 0) AS Isrecurring,
                    CAST(ed.StartDate AS DATE) AS RawStartDate,
                    TRY_CAST(NULLIF(ed.StartTime, '') AS DATETIME) AS RawStartTime,
                    CAST(ed.EndDate AS DATE) AS RawEndDate,
                    TRY_CAST(NULLIF(ed.EndTime, '') AS DATETIME) AS RawEndTime,
                    CASE WHEN et.Coursedocid IS NOT NULL THEN 1 ELSE 0 END AS IsETicket
                FROM Events_Default ed
                INNER JOIN ProcessInfo pi ON ed.DocId = pi.PrimaryDocId
                INNER JOIN [State] s ON s.StateId = pi.CurrentStateId
                LEFT JOIN EticketCTE et ON et.Coursedocid = ed.DocId
                WHERE 
                    s.StateId IN (21,22,56) AND 
                    ed.LocationType <> 'shop' AND 
                    COALESCE(ed.Isrecurring,0) = 0 AND 
                    ISNULL(ed.OwningEntityid, 0) = @ClubDocId
            ),
            RecurringEventsRaw AS (
                SELECT
                    ed.DocId,
                    ed.Location,
                    NULL AS Postcode,
                    ed.EventName,
                    ed.Address1,
                    ed.Country,
                    ed.EventLocation,
                    ed.County,
                    ed.Town,
                    ed.Timezone,
                    ed.RepositoryId,
                    COALESCE(ed.Isrecurring, 0) AS Isrecurring,
                    CAST(od.ScheduleDate AS DATE) AS RawStartDate,
                    TRY_CAST(NULLIF(od.RecurringStartTime,'') AS DATETIME) AS RawStartTime,
                    CAST(od.ScheduleEndDate AS DATE) AS RawEndDate,
                    TRY_CAST(NULLIF(od.RecurringEndTime,'') AS DATETIME) AS RawEndTime,
                    CASE WHEN et.Coursedocid IS NOT NULL THEN 1 ELSE 0 END AS IsETicket,
                    ROW_NUMBER() OVER (PARTITION BY ed.DocId ORDER BY od.ScheduleDate DESC) AS RowNum
                FROM Events_Default ed
                INNER JOIN ProcessInfo pi ON ed.DocId = pi.PrimaryDocId
                INNER JOIN [State] s ON s.StateId = pi.CurrentStateId
                LEFT JOIN EventRecurringScheduleInterval od ON ed.DocId = od.EventDocId
                LEFT JOIN EticketCTE et ON ed.DocId = et.Coursedocid
                WHERE 
                    s.StateId IN (21,22,56) AND 
                    ed.LocationType <> 'shop' AND 
                    COALESCE(ed.Isrecurring,0) = 1 AND 
                    ISNULL(ed.OwningEntityid, 0) = @ClubDocId
            ),
            RecurringEvents AS (
                SELECT * FROM RecurringEventsRaw WHERE RowNum = 1
            ),
            UnifiedEvents AS (
                SELECT * FROM BaseEvents
                UNION ALL
                SELECT DocId, Location, Postcode, EventName, Address1, Country,
                       EventLocation, County, Town, Timezone, RepositoryId,
                       Isrecurring, RawStartDate, RawStartTime, RawEndDate, RawEndTime, IsETicket
                FROM RecurringEvents
            ),
            EventDatetimes AS (
                SELECT
                    *,
                    DATEADD(SECOND, 0, CAST(RawStartDate AS DATETIME) + CAST(RawStartTime AS DATETIME)) AS EventFlagStartDate,
                    DATEADD(SECOND, 0, CAST(RawEndDate AS DATETIME) + CAST(RawEndTime AS DATETIME)) AS EventFlagEndDate
                FROM UnifiedEvents
            ),
            SoftKeyPrep AS (
                SELECT
                    *,
                    LOWER(LTRIM(RTRIM(EventName))) AS NormEventName,
                    LOWER(ISNULL(Location,''))     AS NormLocation,
                    CONVERT(VARCHAR(10), EventFlagStartDate, 23) AS StartDateKey,
                    CONVERT(VARCHAR(10), EventFlagEndDate, 23) AS EndDateKey,
                    HASHBYTES('SHA2_256',
                        LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(100), DocId))))
                    ) AS SoftKeyHash
                FROM EventDatetimes
            ),
            Deduped AS (
                SELECT *,
                       ROW_NUMBER() OVER (
                         PARTITION BY SoftKeyHash
                         ORDER BY EventFlagStartDate DESC, DocId DESC
                       ) AS DupRank
                FROM SoftKeyPrep
            ),
            OrderedFinal AS (
                SELECT *
                FROM Deduped
                WHERE DupRank = 1
                 @sqlWhere    -- Optional search filter 

            ),
            FinalData AS (
                SELECT 
                    *,
                    ROW_NUMBER() OVER (ORDER BY EventFlagStartDate {request.SortOrder}) AS RowId,
                    COUNT(*) OVER() AS TotalCount
                FROM OrderedFinal
            )
            SELECT 
                DocId,
                [Location],
                Postcode,
                EventName,
                Address1,
                Country,
                EventLocation,
                County,
                Town,
                Timezone,
                RepositoryId,
                Isrecurring,
                IsETicket,
                x.gm_offset,
                x.abbreviation,
                RawStartDate AS StartDate,
                RawEndDate AS EndDate,
                CONVERT(VARCHAR(10), RawStartDate, 23) AS StartDateString,
                CONVERT(VARCHAR(10), RawEndDate, 23) AS EndDateString,
                CONCAT(
                  CONVERT(VARCHAR(10), RawStartDate, 23), ' ',
                  FORMAT(COALESCE(RawStartTime, '00:00'), 'HH:mm'),
                  ' ', x.abbreviation
                ) AS EventStartDate,
                CONCAT(
                  CONVERT(VARCHAR(10), RawEndDate, 23), ' ',
                  FORMAT(COALESCE(RawEndTime, '00:00'), 'HH:mm'),
                  ' ', x.abbreviation
                ) AS EventEndDate,
                FORMAT(COALESCE(RawStartTime, '00:00'), 'HH:mm')  AS StartTime,
                FORMAT(COALESCE(RawEndTime, '00:00'), 'HH:mm') as EndTime,
                EventFlagStartDate,
                EventFlagEndDate,
                CASE
                  WHEN CAST(SYSUTCDATETIME() AS DATE) BETWEEN CAST(EventFlagStartDate AS DATE) AND CAST(EventFlagEndDate AS DATE) THEN 1
                  ELSE 0
                END AS IsTodayEvent,
                RowId,
                TotalCount
            FROM FinalData
            OUTER APPLY (
                SELECT TOP 1 gm_offset, abbreviation
                FROM Timezone
                WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01', RawStartDate) AS BIGINT) * 3600
                  AND zone_id = Timezone
                ORDER BY time_start DESC
            ) AS x
            WHERE RowId BETWEEN {nextId} AND {dataSize};";
        }
        private Task GetEventLocation(IList<IDictionary<string, object>> events, CancellationToken cancellationToken)
        {


            foreach (var evnt in events)
            {
                evnt["EventLocation"] = evnt["County"] + "," + evnt["Postcode"] + "," + evnt["Country"];
                //remove 2 keys
                evnt.Remove("Directlink");
                evnt.Remove("RepositoryId");
            }

            return Task.CompletedTask;
        }

    }
}
