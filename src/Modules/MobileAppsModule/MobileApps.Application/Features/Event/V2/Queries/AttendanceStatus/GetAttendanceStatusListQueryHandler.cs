using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.AttendanceStatus
{
    class GetAttendanceStatusListQueryHandler : IRequestHandler<GetAttendanceStatusListQuery, List<Dictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public GetAttendanceStatusListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<Dictionary<string, object>>> Handle(GetAttendanceStatusListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select * from AttendanceStatus";

            var result = await _readRepository.Value.GetListAsync(sql, null, null, "text");
            return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(result));
        }
    }
}
