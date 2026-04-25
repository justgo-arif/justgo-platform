using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventTicketTypeList 
{
    class GetRecurringEventTicketTypeListQueryHandler : IRequestHandler<GetRecurringEventTicketTypeListQuery, List<Dictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public GetRecurringEventTicketTypeListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<Dictionary<string, object>>> Handle(GetRecurringEventTicketTypeListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select Products_Default.Name,  

                        CourseBooking_Default.Productdocid as DocId,
                        CAST(CourseBooking_Default.Coursedocid AS BIGINT) AS CourseDocId,
                        CASE 
                        WHEN Products_Default.Wallettemplateid > 0 THEN 1 
                        ELSE 0 
                        END AS IsEticket
                        from CourseBooking_Default 
                        inner join EventRecurringScheduleTicket on EventRecurringScheduleTicket.TicketDocId = CourseBooking_Default.Productdocid
                        inner join Products_Default on Products_Default.DocId = EventRecurringScheduleTicket.TicketDocId
                        inner join EventRecurringScheduleInterval  on EventRecurringScheduleTicket.EventRecurringScheduleIntervalRowId = EventRecurringScheduleInterval.RowId
                        inner join Events_Default on Events_Default.DocId = EventRecurringScheduleInterval.EventDocId
                        inner join ProcessInfo on CourseBooking_Default.DocId = ProcessInfo.PrimaryDocId
                        inner join [state] on [State].StateId = ProcessInfo.CurrentStateId
						where events_Default.Isrecurring =1  AND [State].StateId in (23,24,25) AND EventRecurringScheduleInterval.RowId=@EventRecurringScheduleIntervalRowId";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EventRecurringScheduleIntervalRowId", request.RowId);
            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(result));
        }
    }
}
