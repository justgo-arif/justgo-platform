using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetMemberSessionLicenses
{
    class MemberSessionBookingsQueryHandler : IRequestHandler<MemberSessionBookingsQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public MemberSessionBookingsQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(MemberSessionBookingsQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT jpr.PaymentReferenceId,
	                            IsNull(IsTrial, 0)                        AS IsTrial,
	                            mdb.Tempfirstname + ' ' + mdb.Tempsurname AS NAME,
	                            jpr.BookingDate                           AS BookingDate,
	                            c.ClassGuid as ClassGuid, jpr.CourseBookingId, c.OwningEntityId
                            FROM JustGoBookingPaymentReference jpr
	                            INNER JOIN members_Default mdb
	                            ON mdb.DocId = jpr.EntityDocId
	                            INNER JOIN JustGoBookingClassSession cs
	                            ON cs.SessionId = jpr.SessionId
	                            INNER JOIN JustGoBookingClass c
	                            ON c.ClassId = cs.ClassId
                            where jpr.SessionId=@SessionId and mdb.DocId = @MemberDocId;";

            var queryParam = new DynamicParameters();
            queryParam.Add("SessionId", request.SessionId);
            queryParam.Add("MemberDocId", request.MemberDocId);

            var result = await _readRepository.Value.GetListAsync(sql, queryParam, null, "text");
            return JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));
        }
    }
}
