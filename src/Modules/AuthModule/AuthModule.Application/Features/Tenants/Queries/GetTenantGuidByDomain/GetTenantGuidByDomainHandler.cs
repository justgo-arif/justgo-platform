using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantGuidByDomain
{
    public class GetTenantGuidByDomainHandler : IRequestHandler<GetTenantGuidByDomainQuery, string>
    {
        private readonly IUtilityService _utilityService;
        public GetTenantGuidByDomainHandler(IUtilityService utilityService)
        {
            _utilityService = utilityService;
        }
        public async Task<string> Handle(GetTenantGuidByDomainQuery request, CancellationToken cancellationToken = default)
        {
            var tenantClientId = await _utilityService.GetTenantClientIdByDomain(cancellationToken);
            if (string.IsNullOrWhiteSpace(tenantClientId))
                throw new InvalidOperationException("Could not resolve tenant from the current request domain.");

            return tenantClientId;
        }
    }
}
