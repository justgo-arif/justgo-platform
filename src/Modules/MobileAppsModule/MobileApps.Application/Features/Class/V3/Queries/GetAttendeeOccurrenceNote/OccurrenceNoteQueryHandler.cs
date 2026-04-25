using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Classes;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetAttendeeOccurrenceNote    
{
    class OccurrenceNoteQueryHandler : IRequestHandler<OccurrenceNoteQuery, IDictionary<string, object>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public OccurrenceNoteQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IDictionary<string, object>> Handle(OccurrenceNoteQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select * from JustGoBookingAttendeeDetails ad
                            join JustGoBookingAttendeeDetailNote dn on ad.AttendeeDetailsId=dn.AttendeeDetailsId
                            where ad.AttendeeId=@AttendeeId AND OccurenceId=@OccurrenceId";        

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AttendeeId", request.AttendeeId);
            queryParameters.Add("@OccurrenceId", request.OccurrenceId);

            var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
            return JsonConvert.DeserializeObject<IDictionary<string, object>>(JsonConvert.SerializeObject(result))??new Dictionary<string, object>();

        }

       

    }
}
