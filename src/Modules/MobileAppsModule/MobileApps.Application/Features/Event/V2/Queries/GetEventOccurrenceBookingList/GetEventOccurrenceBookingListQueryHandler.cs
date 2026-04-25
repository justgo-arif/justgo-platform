using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventOccurrenceBookingList
{
    class GetEventOccurrenceBookingListQueryHandler : IRequestHandler<GetEventOccurrenceBookingListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetEventOccurrenceBookingListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetEventOccurrenceBookingListQuery request, CancellationToken cancellationToken)
        {
            string sql = EventOccurrenceAttendeeListSql(request.SortOrder);
            string sqlWhere = " ed.Isrecurring =1  and st.StateId in (23,24,25) AND ersi.RowId=@EventRecurringScheduleIntervalRowId AND CAST(ersod.OccurrenceDate AS DATE) = CAST(@OccurrenceDate AS DATE)";



            DateTime dateTime = string.IsNullOrEmpty(request.OccuranceDate) ? DateTime.UtcNow : DateTime.Parse(request.OccuranceDate);
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EventRecurringScheduleIntervalRowId", request.OccuranceRowId);
            queryParameters.Add("@OccurrenceDate", dateTime);
            queryParameters.Add("@NextId", request.NextId <= 0 ? 0 : request.NextId + 1);
            queryParameters.Add("@DataSize", request.DataSize <= 0 ? 100 : (request.NextId + request.DataSize));


            if (!string.IsNullOrEmpty(request.AttendeeName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND CONCAT(u.FirstName, ' ', u.LastName) LIKE '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.AttendeeName);
            }
            if (request.TicketTypes.Count() > 0)
            {
                sqlWhere = sqlWhere + @" AND pd.DocId IN @TicketIDs";
                queryParameters.Add("@TicketIDs", request.TicketTypes);
            }
            if (request.AttendeeStatuses.Count() > 0)
            {

                if (request.AttendeeStatuses.Count() == 1 && request.AttendeeStatuses.Contains("pending", StringComparer.OrdinalIgnoreCase))
                {
                    sqlWhere = sqlWhere + @" AND (ea.AttandanceStatus IS NULL OR ea.AttandanceStatus = '' OR ea.AttandanceStatus = 'Pending')";
                }
                else if (request.AttendeeStatuses.Count() > 1 && request.AttendeeStatuses.Contains("pending", StringComparer.OrdinalIgnoreCase))
                {
                    sqlWhere = sqlWhere + @" AND (ea.AttandanceStatus IN @StatusList OR ea.AttandanceStatus IS NULL OR ea.AttandanceStatus = '')";
                    queryParameters.Add("@StatusList", request.AttendeeStatuses);
                }
                else
                {
                    sqlWhere = sqlWhere + @" AND ea.AttandanceStatus IN @StatusList";
                    queryParameters.Add("@StatusList", request.AttendeeStatuses);
                }
            }

            sql = sql.Replace("@sqlCountWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
          
            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

        }
        private static string EventOccurrenceAttendeeListSql(string sortOrder)
        {
            return $@";WITH BaseCTE AS (
            SELECT 
                cbd.DocId,
                CAST(ersi.RowId AS BIGINT) AS RowId,
                CONCAT(u.FirstName, ' ', u.LastName) AS UserName,
                u.UserId,
                u.Gender,
                u.ProfilePicURL,
                ed.EventName,
                pd.Name AS Ticket,
                cbd.Quantity AS TicketCount,
                ea.AttandanceDate,
                ISNULL(ea.AttandanceStatus, 'Pending') AS AttandanceStatus,
                ISNULL(ea.Note, '') AS Note,
                ersi.ScheduleDate,
                ersi.RecurringStartTime,
                ed.Timezone,
                ea.CheckedInAt,
                ersod.OccurrenceDate,
                ROW_NUMBER() OVER (
                    PARTITION BY cbd.DocId
                    ORDER BY ersod.OccurrenceDate DESC -- Select most recent occurrence per DocId
                ) AS rn
            FROM CourseBooking_Default cbd
            INNER JOIN EventRecurringScheduleTicket erst 
                ON erst.TicketDocId = cbd.Productdocid
            INNER JOIN Products_Default pd 
                ON pd.DocId = erst.TicketDocId
            INNER JOIN EventRecurringScheduleInterval ersi 
                ON erst.EventRecurringScheduleIntervalRowId = ersi.RowId
            INNER JOIN EventRecurringScheduleOccurrenceDate ersod 
                ON ersi.RowId = ersod.ScheduleId
            INNER JOIN Events_Default ed 
                ON ed.DocId = ersi.EventDocId
            INNER JOIN [user] u 
                ON u.MemberDocId = cbd.Entityid
            LEFT JOIN EventAttendances ea 
                ON ea.CourseBookingDocId = cbd.DocId
            INNER JOIN ProcessInfo pi 
                ON cbd.DocId = pi.PrimaryDocId
            INNER JOIN [state] st 
                ON st.StateId = pi.CurrentStateId
            WHERE @sqlCountWhere
        ),
        NumberedCTE AS (
            SELECT *,
                ROW_NUMBER() OVER(ORDER BY UserName {sortOrder}) AS RowNum,
                COUNT(*) OVER() AS TotalCount
            FROM BaseCTE
            WHERE rn = 1 -- Only the top row per DocId (most recent OccurrenceDate)
        ),
        PagedCTE AS (
            SELECT *
            FROM NumberedCTE
            WHERE RowNum BETWEEN @NextId AND @DataSize
        )
        SELECT 
            p.DocId AS BookingDocId,
            p.RowId,
            p.TicketCount,
            p.UserName,
            p.UserId,
            p.Gender,
            p.ProfilePicURL,
            p.EventName,
            p.Ticket,
            p.AttandanceDate,
            p.AttandanceStatus,
            p.Note,
            FORMAT(p.ScheduleDate, 'yyyy-MM-dd') AS ScheduleDate,
            p.RecurringStartTime, p.Timezone,
            dbo.[GET_UTC_LOCAL_DATE_TIME](CAST(p.ScheduleDate AS DATETIME) + CAST(p.RecurringStartTime AS DATETIME),p.Timezone) AS StartDate,
            FORMAT(dbo.[GET_UTC_LOCAL_DATE_TIME](p.CheckedInAt, p.Timezone), 'hh:mm tt, dd MMM yyyy') AS CheckedInAt,
            p.RowNum AS RowNumberId,
            p.TotalCount
        FROM PagedCTE p
        ORDER BY p.RowNum;";
        }

       
        
    }
}
