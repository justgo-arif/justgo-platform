using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Classes;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetClassSessionList
{
    class GetClassSessionQueryHandler : IRequestHandler<GetClassSessionQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public GetClassSessionQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetClassSessionQuery request, CancellationToken cancellationToken)
        {
            string subSql = "";
            string sql = @"WITH DaysOfWeek AS (
                SELECT v.DayOfWeek, v.FullDayName
                FROM (VALUES
                    ('Mon', 'Monday'),
                    ('Tue', 'Tuesday'),
                    ('Wed', 'Wednesday'),
                    ('Thu', 'Thursday'),
                    ('Fri', 'Friday'),
                    ('Sat', 'Saturday'),
                    ('Sun', 'Sunday')
                ) v(DayOfWeek, FullDayName)
            )

            SELECT 
                cs.SessionId,
                cs.SessionType,
                cs.[Name],
                cs.TimeZoneId,
                cs.VenueId,
                CONCAT(v.Name, '-', COALESCE(v.Address1, ''), ',', COALESCE(v.Country, ''), ',', COALESCE(v.County, '')) AS [Location],
                CONCAT(FORMAT(DATEADD(SECOND, x.gm_offset, tm.StartDate), 'd MMM yyyy'), ' ', x.abbreviation) AS SessionStartDate,
                CONCAT(FORMAT(DATEADD(SECOND, x.gm_offset, tm.EndDate), 'd MMM yyyy'), ' ', x.abbreviation) AS SessionEndDate,
            (
                    SELECT TOP 1 
                        CASE 
                            WHEN CAST(SYSUTCDATETIME() AS DATE) > CAST(o.EndDate AS DATE) THEN -1
                            WHEN CAST(SYSUTCDATETIME() AS DATE) BETWEEN CAST(o.StartDate AS DATE) AND CAST(o.EndDate AS DATE) THEN 0
                            WHEN CAST(SYSUTCDATETIME() AS DATE) < CAST(o.StartDate AS DATE) THEN 1
                        END
                    FROM JustGoBookingClassSessionSchedule ss
                    INNER JOIN JustGoBookingScheduleOccurrence o ON ss.SessionScheduleId = o.ScheduleId
                    WHERE ss.SessionId = cs.SessionId
                    ORDER BY o.StartDate DESC
                ) AS DateGroupFlag,
                cs.TermId,

                (
                    SELECT 
                        ss.DayOfWeek, 
                        d.FullDayName
                    FROM JustGoBookingClassSessionSchedule ss
                    INNER JOIN DaysOfWeek d ON ss.DayOfWeek = d.DayOfWeek
                    WHERE ss.SessionId = cs.SessionId
                    FOR JSON PATH
                ) AS Schedules

            FROM JustGoBookingClassSession cs

            INNER JOIN Venue_Default v ON v.DocId = cs.VenueId
            INNER JOIN JustGoBookingClassTerm tm ON tm.ClassTermId = cs.TermId

            OUTER APPLY (
                SELECT TOP 1 gm_offset, abbreviation 
                FROM Timezone 
                WHERE time_start <= DATEDIFF(SECOND, '1970-01-01 00:00:00', tm.StartDate)
                  AND zone_id = cs.TimeZoneId 
                ORDER BY time_start DESC
            ) AS x

            WHERE 
                cs.ClassId = @ClassId
                AND EXISTS (
                    SELECT 1
                    FROM JustGoBookingClassSessionSchedule ss
                    INNER JOIN JustGoBookingScheduleOccurrence o ON ss.SessionScheduleId = o.ScheduleId
                    WHERE ss.SessionId = cs.SessionId
                     @subSQL
                )

            ORDER BY SessionStartDate DESC;";        

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClassId", request.ClassId);

            if (!string.IsNullOrEmpty(request.SessionName.Trim()))
            {
                sql = sql + @" AND v.Name Like '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.SessionName.Trim());
            }

            if (!string.IsNullOrEmpty(request.SessionStartDate) && string.IsNullOrEmpty(request.SessionEndDate))
            {

                DateTime startDate = DateTime.Parse(request.SessionStartDate);
                var start = startDate.ToString("yyyy-MM-dd 00:00:00:000");
                var end = startDate.ToString("yyyy-MM-dd 23:59:59:000");

                subSql = @" AND CAST(o.StartDate AS DATETIME) <= CAST(@EndDate AS DATETIME)  AND CAST(o.StartDate AS DATETIME) >= CAST(@StartDate AS DATETIME)";
                queryParameters.Add("@StartDate", start);
                queryParameters.Add("@EndDate", end);
            }

            if (!string.IsNullOrEmpty(request.SessionEndDate) && !string.IsNullOrEmpty(request.SessionStartDate))
            {

                DateTime startDate = DateTime.Parse(request.SessionStartDate);
                DateTime endDate = DateTime.Parse(request.SessionEndDate);
                var start = startDate.ToString("yyyy-MM-dd 00:00:00:000");
                var end = endDate.ToString("yyyy-MM-dd 23:59:59:000");

                subSql = @" AND CAST(o.StartDate AS DATETIME) <= CAST(@EndDate AS DATETIME)  AND CAST(o.EndDate AS DATETIME) >= CAST(@StartDate AS DATETIME)";
                queryParameters.Add("@StartDate", start);
                queryParameters.Add("@EndDate", end);
            }

            sql = sql.Replace("@subSQL", subSql);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            var mappingResult = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

            foreach (var item in mappingResult)
            {
                if(item.TryGetValue("Schedules", out var scheduleList))
                {
                    item["Schedules"] = JsonConvert.DeserializeObject<List<DayInfo>>(scheduleList.ToString());
                }
            }

            return mappingResult;
        }

       

    }
}
