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

namespace MobileApps.Application.Features.Class.V2.Queries.GetAttendeeNoteList
{
    class NoteListQueryHandler : IRequestHandler<NoteListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public NoteListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(NoteListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select * from JustGoBookingAttendeeDetails ad
                            join JustGoBookingAttendeeDetailNote dn on ad.AttendeeDetailsId=dn.AttendeeDetailsId
                            where ad.AttendeeId=@AttendeeId";        

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AttendeeId", request.AttendeeId);

            

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

        }

       

    }
}
