using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventBookingList
{
    class GetEventBookingCountQueryHandler : IRequestHandler<GetEventBookingCountQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public GetEventBookingCountQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetEventBookingCountQuery request, CancellationToken cancellationToken)
        {
            string sql = EventBookingCountSql();
            string sqlWhere = "[State].StateId IN (23, 24, 25) AND cbd.Coursedocid=@EventDocId";

            var queryParameters = new DynamicParameters();

            queryParameters.Add("@EventDocId", Convert.ToDecimal(request.EventDocId).ToString());

            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");


            return AttendanceCountShared.MergeStatusLists(result);
        }
        private string EventBookingCountSql()
        {
            return @";WITH StatusCTE AS
            (
                SELECT 
                    cbd.DocId,
                    COALESCE(NULLIF(MAX(EventAttendances.AttandanceStatus), ''), 'Pending') AS StatusName
                FROM 
                    CourseBooking_Default AS cbd
                INNER JOIN 
                    Products_Default ON Products_Default.DocId = cbd.Productdocid
                INNER JOIN
                    Events_Default ON Events_Default.DocId = cbd.Coursedocid
                INNER JOIN
                    [user] ON [user].MemberDocId = cbd.Entityid
                INNER JOIN
                    ProcessInfo ON cbd.DocId = ProcessInfo.PrimaryDocId
                INNER JOIN
                    [state] ON [state].StateId = ProcessInfo.CurrentStateId
                LEFT JOIN 
                    EventAttendances ON EventAttendances.CourseBookingDocId = cbd.DocId
                WHERE @sqlWhere
                GROUP BY 
                    cbd.DocId
            )
            SELECT 
                StatusName,
                COUNT(*) AS StatusCount
            FROM 
                StatusCTE
            GROUP BY 
                StatusName;";
        }

    }
}
