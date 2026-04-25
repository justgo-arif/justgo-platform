using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.Class.V2.Queries.GetSessionOccurrenceList;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetSessionOccurrenceList
{
    class GetSessionOccurrenceListQueryHandler : IRequestHandler<GetSessionOccurrenceListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public GetSessionOccurrenceListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetSessionOccurrenceListQuery request, CancellationToken cancellationToken)
        {
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

                        SELECT so.OccurrenceId,so.ScheduleId,cs.DayOfWeek,
                        so.StartDate,so.EndDate,
                        FORMAT(CAST(so.StartDate AS DATE), 'd MMMM') AS ShortDate,
                            CONCAT(FORMAT(DATEADD(SECOND, x.gm_offset, so.StartDate), 'hh:mm tt'), ' ', x.abbreviation) AS SessionStartTime,
                            CONCAT(FORMAT(DATEADD(SECOND, x.gm_offset, so.EndDate), 'hh:mm tt'), ' ', x.abbreviation) AS SessionEndTime,
                            (
                            SELECT  
                                d.FullDayName
                            FROM DaysOfWeek d
                            WHERE d.DayOfWeek = cs.DayOfWeek
                        ) AS FullDayName,
                     
                        CASE 
                            WHEN CAST(so.StartDate AS DATE) < CAST(SYSUTCDATETIME() AS DATE) THEN -1
                            WHEN CAST(so.StartDate AS DATE) = CAST(SYSUTCDATETIME() AS DATE) THEN 0
                            ELSE 1
                        END AS DateGroupFlag

                        FROM JustGoBookingClassSessionSchedule cs
                        left JOIN JustGoBookingScheduleOccurrence so ON cs.SessionScheduleId = so.ScheduleId

                        OUTER APPLY (
                            SELECT TOP 1 gm_offset, abbreviation 
                            FROM Timezone 
                            WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01 00:00:00', so.StartDate) AS bigint) * 60 * 60
                                AND zone_id = (select s.TimeZoneId from JustGoBookingClassSession s where  s.SessionId=cs.SessionId) 
                            ORDER BY time_start DESC
                        ) AS x

                        OUTER APPLY (
                            SELECT TOP 1 gm_offset, abbreviation 
                            FROM Timezone 
                            WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01 00:00:00', so.EndDate) AS bigint) * 60 * 60
                                AND zone_id = (select s.TimeZoneId from JustGoBookingClassSession s where  s.SessionId=cs.SessionId) 
                            ORDER BY time_start DESC
                        ) AS y

                        where cs.SessionId=@SessionId AND cs.IsDeleted <> 1 AND so.OccurrenceId Not In (select OccurrenceId from JustGoBookingAdditionalHoliday where SessionId=cs.SessionId AND OccurrenceId=so.OccurrenceId)  
                        ORDER BY so.OccurrenceId, so.StartDate;";        

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@SessionId", request.SessionId);

         
            if (!string.IsNullOrEmpty(request?.DayOfTheWeek?.Trim()))
            {
                sql = sql + @" AND cs.DayOfWeek=@DayOfWeek";
                queryParameters.Add("@DayOfWeek", request.DayOfTheWeek.Trim());
            }

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));
        }
    }
}
