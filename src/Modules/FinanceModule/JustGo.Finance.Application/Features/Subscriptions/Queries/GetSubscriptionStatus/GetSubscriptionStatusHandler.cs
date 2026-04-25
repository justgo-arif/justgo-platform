using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionStatus
{

    public class GetSubscriptionStatusHandler : IRequestHandler<GetSubscriptionStatusQuery, List<LookupIntDto>>
    {
        private readonly LazyService<IReadRepository<LookupIntDto>> _readRepository;

        public GetSubscriptionStatusHandler(LazyService<IReadRepository<LookupIntDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<LookupIntDto>> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
        {

            var sql = @"SELECT 
                        v.Id,
                        CASE v.Id
                        WHEN 2 THEN 'Active'
                        WHEN 3 THEN 'Complete'
                        WHEN 6 THEN 'Cancelled'
                        ELSE 'unknown'
                        END AS Name
                        FROM (VALUES (2), (3), (6)) AS v(Id);";
            return (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, "text")).ToList();
        }
    }

}
