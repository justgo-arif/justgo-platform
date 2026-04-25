using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V2.Queries.GetMemberSessionLicenses
{
    class MemberSessionPaymentQueryHandler : IRequestHandler<MemberSessionPaymentQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public MemberSessionPaymentQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(MemberSessionPaymentQuery request, CancellationToken cancellationToken)
        {
            string sql = @"GetAttendeePaymentDetailsBySessionId";

            var queryParam = new DynamicParameters();
            queryParam.Add("SessionId", request.SessionId);
            queryParam.Add("AttendeeId", request.AttendeeId);
            queryParam.Add("ProductId", request.ProductId);

            var result = await _readRepository.Value.GetListAsync(sql, queryParam, null, "sp");
            return JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));
        }
    }
}
