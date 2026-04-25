using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventBookingList
{
    class GetEventOccuranceBookingCountQueryHandler : IRequestHandler<GetEventOccuranceBookingCountQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public GetEventOccuranceBookingCountQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetEventOccuranceBookingCountQuery request, CancellationToken cancellationToken)
        {
            DateTime dateTime = DateTime.Parse(request.OccuranceDate);
            string sql = EventOccuranceBookingCountSql();

            string sqlWhere = "events_Default.Isrecurring =1 and [State].StateId in (23,24,25) AND ersi.RowId=@IntervalRowId AND CAST(ersi.ScheduleDate AS DATE) = CAST(@OccuranceDate AS DATE)";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@IntervalRowId", request.RowId);
            queryParameters.Add("@OccuranceDate", dateTime);


            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            return AttendanceCountShared.MergeStatusLists(result);
        }
        private string EventOccuranceBookingCountSql()
        {
            return @"select  CourseBooking_Default.DocId as BookingDocId,
                    MAX(CAST(ersi.RowId AS BIGINT)) AS RowId,
         
                    MAX(ea.AttandanceDate) AS AttandanceDate,
                    COUNT(DISTINCT CourseBooking_Default.DocId) AS StatusCount,
                   
                    MAX(CASE 
						WHEN ea.AttandanceStatus IS NULL OR ea.AttandanceStatus = '' THEN 'Pending' 
						ELSE ea.AttandanceStatus 
					END) AS StatusName
                  
						

                    from  CourseBooking_Default 

                    inner join EventRecurringScheduleTicket erst on erst.TicketDocId = CourseBooking_Default.Productdocid
                    inner join Products_Default on Products_Default.DocId = erst.TicketDocId
                    inner join EventRecurringScheduleInterval as ersi  on erst.EventRecurringScheduleIntervalRowId = ersi.RowId
                    inner join Events_Default on Events_Default.DocId = ersi.EventDocId

                    --new start
                    inner join Document dd on CourseBooking_Default.DocId=dd.DocId
                    inner join Members_Default md on md.DocId = CourseBooking_Default.Entityid
                    --new end
                    inner join Document mdoc on mdoc.DocId = md.DocId

                    inner join [user] on [user].MemberDocId = md.DocId
                    inner join ProcessInfo on CourseBooking_Default.DocId = ProcessInfo.PrimaryDocId
                    inner join [state] on [State].StateId = ProcessInfo.CurrentStateId
                    left join EventRecurringScheduleOccurrenceDate ersod on ersi.RowId = ersod.ScheduleId
                    left join EventAttendances as  ea on ea.CourseBookingDocId = CourseBooking_Default.DocId 

                    WHERE @sqlWhere
            
                    GROUP BY CourseBooking_Default.DocId";
        }

    }
}
