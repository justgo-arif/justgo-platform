using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventBookingList
{
    class GetEventBookingListQueryHandler : IRequestHandler<GetEventBookingListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private readonly LazyService<IReadRepository<dynamic>> _readDynamicRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetEventBookingListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator, ISystemSettingsService systemSettingsService, LazyService<IReadRepository<dynamic>> readDynamicRepository)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
            _readDynamicRepository = readDynamicRepository;
        }

        public async Task<IList<IDictionary<string, object>>> Handle(GetEventBookingListQuery request, CancellationToken cancellationToken)
        {
            string sqlCteWithJoinDataList = CTEWithJoinQuery(request.SortOrder);
            string sqlWhere = $@" st.StateId in (23,24,25) AND cbd.Coursedocid = @EventDocId ";
          
            var queryParameters = new DynamicParameters();

            queryParameters.Add("@EventDocId", Convert.ToDecimal(request.EventDocId));
            queryParameters.Add("@NextId", request.NextId <=0 ? 0 : request.NextId+1);
            queryParameters.Add("@DataSize", request.DataSize <= 0 ? 100 : (request.NextId + request.DataSize));
          

            if (!string.IsNullOrEmpty(request.AttendeeName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND CONCAT(u.FirstName,' ', u.LastName) LIKE '%'+@Filter+'%'";
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

            sqlCteWithJoinDataList = sqlCteWithJoinDataList.Replace("@sqlCountWhere", sqlWhere);
           
            var result = await _readDynamicRepository.Value.GetListAsync(sqlCteWithJoinDataList, queryParameters, null, "text");
            var bookingList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result)) ?? new List<IDictionary<string, object>>();

            
            return bookingList;
        }
        private string CTEWithJoinQuery(string sortOrder)
        {
            return $@";WITH RankedCTE AS (
            SELECT 
                cbd.DocId AS BookingDocId,
                CAST(cbd.Coursedocid AS BIGINT) AS EventDocId, 
                CONCAT(u.FirstName, ' ', u.LastName) AS UserName,
                u.UserId,
                u.Gender,
                ISNULL(u.ProfilePicURL, '') AS ProfilePicURL,
                ed.EventName,
                pd.Name AS Ticket,
                CAST(cbd.Quantity AS INT) AS TicketCount,
                ea.AttandanceDate,
                ea.AttandanceStatus,
                FORMAT(ed.StartDate, 'yyyy-MM-dd') AS StartDate,
                ed.StartTime,
                ISNULL(ea.Note, '') AS Note,
                ed.Timezone,
                ea.CheckedInAt,
                ROW_NUMBER() OVER (
                    PARTITION BY cbd.DocId
                    ORDER BY ed.StartDate DESC -- Or your preferred field
                ) AS rn
            FROM CourseBooking_Default cbd
            INNER JOIN Products_Default pd 
                ON pd.DocId = cbd.Productdocid
            INNER JOIN Events_Default ed 
                ON ed.DocId = cbd.Coursedocid
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
            FROM RankedCTE
            WHERE rn = 1 -- Only the top row per BookingDocId
        ),
        PagedCTE AS (
            SELECT *
            FROM NumberedCTE
            WHERE RowNum BETWEEN @NextId AND @DataSize
        )
        SELECT 
            p.BookingDocId,
            p.EventDocId,
            p.TicketCount,
            p.UserName,
            p.UserId,
            p.Gender,
            p.ProfilePicURL,
            p.EventName,
            p.Ticket,
            p.AttandanceDate,
            p.AttandanceStatus,
            p.Note,p.Timezone,
            dbo.[GET_UTC_LOCAL_DATE_TIME](p.StartDate, p.Timezone) AS EventStartDate,
	        FORMAT(dbo.[GET_UTC_LOCAL_DATE_TIME](p.CheckedInAt, p.Timezone), 'hh:mm tt, dd MMM yyyy') AS CheckedInAt,
            p.RowNum as RowNumberId,
            p.TotalCount
        FROM PagedCTE p
        ORDER BY p.RowNum;";
        }
        
    }
}
