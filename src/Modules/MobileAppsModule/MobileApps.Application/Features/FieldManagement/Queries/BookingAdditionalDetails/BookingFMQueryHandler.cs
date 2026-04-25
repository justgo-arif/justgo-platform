using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection
{
    class BookingFMQueryHandler : IRequestHandler<BookingFMQuery, List<BookingSchemaInfo>>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        public BookingFMQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<BookingSchemaInfo>> Handle(BookingFMQuery request, CancellationToken cancellationToken)
        {
            string query = @"select  dci.Type, dci.Config ,
			JSON_VALUE(REPLACE(dci.Config, '$dataFieldInfo', 'fs'), '$.fs.compId') as ItemId
                            from JustGoBookingClassSessionProduct  sp 
                            inner join Products_Default pd on sp.ProductId = pd.DocId and sp.ProductType=1 
                            inner join Products_Datacaptureitems dci  on dci.DocId = pd.DocId
                            where sp.SessionId =@SessionId";

            var param = new DynamicParameters();
            param.Add("@SessionId", request.SessionId);
          

            var result = await _readRepository.Value.GetListAsync(query, param, null, "text");
            string jsonString = JsonConvert.SerializeObject(result);
            return JsonConvert.DeserializeObject<List<BookingSchemaInfo>>(jsonString);
        }
    }
}
