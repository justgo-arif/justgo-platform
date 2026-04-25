using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using MobileApps.Domain.Entities.V2.Event;

namespace MobileApps.Application.Features.Event.V2.Queries.GetRecurringOccuranceBookingDateList
{
    class GetRecurringOccuranceBookingDateListQueryHandler : IRequestHandler<GetRecurringOccuranceBookingDateListQuery, IList<BookingDate>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public GetRecurringOccuranceBookingDateListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<IList<BookingDate>> Handle(GetRecurringOccuranceBookingDateListQuery request, CancellationToken cancellationToken)
        {
            string sql = EventOccuranceAttendyDateListSql();

            var queryParameters = new DynamicParameters();

            queryParameters.Add("@RowId", request.RowId);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            return JsonConvert.DeserializeObject<IList<BookingDate>>(JsonConvert.SerializeObject(result));
        }
        private string EventOccuranceAttendyDateListSql()
        {
            return @"-- CTE: EventsDate - Calculates schedule dates in local timezone and extracts recurrence details
            ;WITH EventsDate AS (
                SELECT
                    ed.Timezone,
                    erIntv.RowId,
		            dbo.[GET_UTC_LOCAL_DATE_TIME](erIntv.ScheduleDate, ed.Timezone)  AS ScheduleDate,
		            dbo.[GET_UTC_LOCAL_DATE_TIME](erIntv.ScheduleEndDate, ed.Timezone) AS ScheduleEndDate,
     
                    JSON_VALUE(ScheduleExpression, '$.Interval') AS Interval,
                    (
                        SELECT
                            STRING_AGG(value, '|')
                        FROM OPENJSON(ScheduleExpression, '$.DayofWeek')
                    ) AS FowllingDays
                FROM
                    Events_Default ed
                    INNER JOIN EventRecurring er ON er.EventDocId = ed.DocId
                    INNER JOIN EventRecurringScheduleInterval erIntv ON erIntv.EventRecurringRowId = er.Recurring_id
  
                WHERE erIntv.RowId = @RowId
            ),

            -- CTE: DateList - Recursively generates all dates between start and end
            DateList AS (
                SELECT
                    RowId,
                    ScheduleDate,
                    ScheduleEndDate,
                    FORMAT(ScheduleDate, 'ddd') AS DayShort
                FROM EventsDate

                UNION ALL

                SELECT
                    RowId,
                    DATEADD(DAY, 1, ScheduleDate),
                    ScheduleEndDate,
                    FORMAT(DATEADD(DAY, 1, ScheduleDate), 'ddd') AS DayShort
                FROM DateList
                WHERE DATEADD(DAY, 1, ScheduleDate) <= ScheduleEndDate
            )
            --select * from DateList
            -- Select valid dates into temp table with formatted day
            SELECT
                DateList.RowId,
                FORMAT(DateList.ScheduleDate, 'ddd dd MMM yy') AS ScheduleDateWithDay,
                DateList.ScheduleDate,
	            FORMAT(DateList.ScheduleEndDate, 'ddd dd MMM yy') AS ScheduleEndDateWithDay,
	            DateList.ScheduleEndDate
            INTO #DateList
            FROM DateList
            INNER JOIN EventsDate ON EventsDate.RowId = DateList.RowId
            WHERE
                1 = CASE
                    WHEN Interval = 'week'
                        AND LEN(EventsDate.FowllingDays) > 0
                        AND DateList.DayShort NOT IN (
                            SELECT value FROM STRING_SPLIT(EventsDate.FowllingDays, '|')
                        )
                    THEN 0
                    ELSE 1
                END
            ORDER BY
                ScheduleDate ASC

            -- Return the result
            SELECT * FROM #DateList

            -- Clean up temp table
            DROP TABLE #DateList";
        }
       
    }
}
