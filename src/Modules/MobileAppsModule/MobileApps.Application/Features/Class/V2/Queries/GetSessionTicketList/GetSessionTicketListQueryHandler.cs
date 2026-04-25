using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V2.Queries.GetSessionTicketList
{
    class GetSessionTicketListQueryHandler : IRequestHandler<GetSessionTicketListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public GetSessionTicketListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<IList<IDictionary<string, object>>> Handle(GetSessionTicketListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select sp.ProductId as Id,pd.[Name],sp.ProductType,
                        CASE 
						WHEN sp.ProductType=1 THEN 'One-off'
						WHEN sp.ProductType=2 THEN 'Trial'
						WHEN sp.ProductType=3 THEN 'Payg'
						ELSE 'Subscription' 
						END AS TypeName

                        from JustGoBookingClassSessionProduct sp 
                        left join Products_Default pd on sp.ProductId=pd.DocId
                        where sp.SessionId=@SessionId AND sp.IsDeleted<>1";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@SessionId",request.SessionId);

            return  JsonConvert.DeserializeObject<IList<IDictionary<string,object>>>(JsonConvert.SerializeObject(await _readRepository.Value.GetListAsync(sql,queryParameters, null, "text")));
        }
    }
}
