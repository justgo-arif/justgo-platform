using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantByDomain
{
    public class GetTenantByDomainQuery: IRequest<Tenant>
    {
        public string TenantDomain { get; set; }
        public GetTenantByDomainQuery(string tenantDomain)
        {
            TenantDomain = tenantDomain;
        }
    }
}
