using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Club.V2.Query.GetClubEventBookingCount
{
    class GetClubBookingCountQueryHandler : IRequestHandler<GetClubBookingCountQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public GetClubBookingCountQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetClubBookingCountQuery request, CancellationToken cancellationToken)
        {
            if (!request.BookingDate.HasValue) request.BookingDate = DateTime.UtcNow;
                string sql = EventBookingCountSql();
            //recurring Event
            string sqlOccurrence = EventOccuranceBookingCountSql();


            string sqlWhere = "s.StateId in (23,24,25) AND IsNull(ed.OwningEntityid,0)=@ClubDocId";
            string sqlRecurringWhere = "ed.Isrecurring =1 and s.StateId in (23,24,25) AND IsNull(ed.OwningEntityid,0)=@ClubDocId";

            //event
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId", Convert.ToDecimal(request.ClubDocId).ToString());
            if (request.BookingDate.HasValue)
            {
                DateTime startDate = (DateTime)request.BookingDate;
                var start = startDate.ToString("yyyy-MM-dd 00:00:00");
                var end = startDate.ToString("yyyy-MM-dd 23:59:59");
                queryParameters.Add("@StartDate", start);
                queryParameters.Add("@EndDate", end);

                //event where adjust for date filter
                sqlWhere = sqlWhere + @" AND (ed.StartDate <= @EndDate  AND ed.EndDate >= @StartDate)";
                //recurring Event where adjust for date filter
                sqlRecurringWhere = sqlRecurringWhere + @" AND (ersi.ScheduleDate <= @EndDate  AND ersi.ScheduleEndDate  >= @StartDate)";
            }

            sql = sql.Replace("@sqlWhere", sqlWhere);
            sqlOccurrence = sqlOccurrence.Replace("@sqlOccurrenceWhere", sqlRecurringWhere);


            var resultEvent = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            var resultOccurrence = await _readRepository.Value.GetListAsync(sqlOccurrence, queryParameters, null, "text");

            return AttendanceCountShared.MergeClubBookingStatusLists(resultEvent, resultOccurrence);
        }


        private string EventBookingCountSql()
        {
            return @"-- Pre-aggregate attendance status per booking
            ;WITH AttendanceAgg AS
            (
                SELECT 
                    cbd.DocId AS CourseBookingDocId,
                    MAX(CASE 
                            WHEN ea.AttandanceStatus IS NULL OR ea.AttandanceStatus = '' THEN 'Pending'
                            ELSE ea.AttandanceStatus
                        END) AS StatusName
                FROM CourseBooking_Default cbd
                LEFT JOIN EventAttendances ea
                    ON ea.CourseBookingDocId = cbd.DocId
                GROUP BY cbd.DocId
            )
            SELECT 
                aa.StatusName,
                COUNT(*) AS StatusCount
            FROM AttendanceAgg aa
            INNER JOIN CourseBooking_Default cbd
                ON cbd.DocId = aa.CourseBookingDocId
            INNER JOIN Products_Default pd 
                ON pd.DocId = cbd.Productdocid
            INNER JOIN Events_Default ed 
                ON ed.DocId = cbd.Coursedocid
            INNER JOIN Members_Default md 
                ON md.DocId = cbd.Entityid
            INNER JOIN [user] u 
                ON u.MemberDocId = md.DocId
            INNER JOIN ProcessInfo pr 
                ON cbd.DocId = pr.PrimaryDocId
            INNER JOIN [state] s 
                ON s.StateId = pr.CurrentStateId
            WHERE @sqlWhere
            GROUP BY aa.StatusName;";
        }

        private string EventOccuranceBookingCountSql()
        {

            return @"WITH AttendanceAgg AS
                (
                    SELECT 
                        CourseBookingDocId,
                        MAX(AttandanceDate) AS MaxAttandanceDate,
                        MAX(CASE 
                                WHEN AttandanceStatus IS NULL OR AttandanceStatus = '' THEN 'Pending'
                                ELSE AttandanceStatus
                            END) AS StatusName
                    FROM EventAttendances
                    GROUP BY CourseBookingDocId
                )
                SELECT
                    cbd.DocId AS BookingDocId,
                    MAX(ersi.RowId) AS MaxRowId,
                    aa.MaxAttandanceDate AS AttandanceDate,
                    COUNT_BIG(*) AS StatusCount,
                    ISNULL(aa.StatusName, 'Pending') AS StatusName
                FROM CourseBooking_Default cbd
                INNER JOIN EventRecurringScheduleTicket erst 
                    ON erst.TicketDocId = cbd.Productdocid
                INNER JOIN Products_Default pd 
                    ON pd.DocId = erst.TicketDocId
                INNER JOIN EventRecurringScheduleInterval ersi  
                    ON erst.EventRecurringScheduleIntervalRowId = ersi.RowId
                INNER JOIN Events_Default ed 
                    ON ed.DocId = ersi.EventDocId
                INNER JOIN [user] u 
                    ON u.MemberDocId = cbd.Entityid
                INNER JOIN ProcessInfo pi 
                    ON cbd.DocId = pi.PrimaryDocId
                INNER JOIN [state] s 
                    ON s.StateId = pi.CurrentStateId
                LEFT JOIN EventRecurringScheduleOccurrenceDate ersod 
                    ON ersi.RowId = ersod.ScheduleId
                LEFT JOIN AttendanceAgg aa
                    ON aa.CourseBookingDocId = cbd.DocId
                 WHERE @sqlOccurrenceWhere
                GROUP BY cbd.DocId, aa.MaxAttandanceDate, aa.StatusName
                ORDER BY cbd.DocId;";

        }

    }
}
