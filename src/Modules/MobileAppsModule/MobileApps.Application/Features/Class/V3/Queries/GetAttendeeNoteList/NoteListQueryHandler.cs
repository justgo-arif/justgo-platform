using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using MobileApps.Domain.Entities.V2.Classes;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetAttendeeNoteList
{
    class NoteListQueryHandler : IRequestHandler<NoteListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public NoteListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(NoteListQuery request, CancellationToken cancellationToken)
        {
            string sql = $@"select 
            ad.AttendeeDetailsId,
            ad.AttendeeId,ad.OccurenceId,ad.AttendeeType,ad.[Status],
            ad.AttendeePaymentId,ad.AttendeeDetailsStatus,
            dbo.[GET_UTC_LOCAL_DATE_TIME](ad.CheckedInAt, {request.TimeZoneId}) as CheckedInAt,
            dn.AttendeeDetailNoteId,dn.Note,
            dbo.[GET_UTC_LOCAL_DATE_TIME](dn.CreatedDate, {request.TimeZoneId}) as CreatedDate,
            dbo.[GET_UTC_LOCAL_DATE_TIME](dn.ModifiedDate,{request.TimeZoneId}) as ModifiedDate

            from JustGoBookingAttendeeDetails ad
            join JustGoBookingAttendeeDetailNote dn on ad.AttendeeDetailsId=dn.AttendeeDetailsId
            where ad.AttendeeId=@AttendeeId
            Order by dn.AttendeeDetailNoteId desc";        

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AttendeeId", request.AttendeeId);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result))??new List<IDictionary<string, object>>();

        }
        

    }
}
