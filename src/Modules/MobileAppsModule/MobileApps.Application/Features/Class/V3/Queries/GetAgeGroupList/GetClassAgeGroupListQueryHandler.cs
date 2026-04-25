using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    class GetClassAgeGroupListQueryHandler : IRequestHandler<GetClassAgeGroupListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;

        public GetClassAgeGroupListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
           
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetClassAgeGroupListQuery request, CancellationToken cancellationToken)
        {

            string sql = @"select ag.Id, ag.[Name] + ' (' + CAST(ag.MinAge AS VARCHAR(10)) + '-' + CAST(ag.MaxAge AS VARCHAR(10)) + ' years)' AS AgeGroupDisplay,ag.MinAge,ag.MaxAge, ag.[Name]
            from JustGoBookingAgeGroup ag 
            where ag.IsActive>0 AND  ag.OwnerId=@ClubDocId;";

         
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId", request.ClubDocId);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));
        }
    }
}
