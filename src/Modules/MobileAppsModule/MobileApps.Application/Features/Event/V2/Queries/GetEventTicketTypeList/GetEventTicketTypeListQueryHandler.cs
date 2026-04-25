using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventTicketTypeList
{
    class GetEventTicketTypeListQueryHandler : IRequestHandler<GetEventTicketTypeListQuery, List<Dictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public GetEventTicketTypeListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<Dictionary<string, object>>> Handle(GetEventTicketTypeListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT DISTINCT 
                            pd.Name,  
                            cbd.Productdocid as DocId,
                            CAST(cbd.Coursedocid AS BIGINT) AS CourseDocId,
                            CASE 
                            WHEN pd.Wallettemplateid > 0 THEN 1 
                            ELSE 0 
                            END AS IsEticket
                        FROM CourseBooking_Default cbd 
                        INNER JOIN products_Default pd ON pd.docid = cbd.productdocid
                        INNER JOIN ProcessInfo ON cbd.DocId = ProcessInfo.PrimaryDocId
                        INNER JOIN [state] ON [state].StateId = ProcessInfo.CurrentStateId
                        WHERE [state].StateId IN (23, 24, 25) and Coursedocid=@EventDocId";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EventDocId", request.EventDocId);
            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(result));
        }
    }
}
