using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Helper;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantClientId
{
    public class GetTenantByTenantClientIdQuery:IRequest<Tenant>
    {
        public string TenantClientId { get; set; }
        public GetTenantByTenantClientIdQuery(string tenantClientId)
        {
            TenantClientId = tenantClientId;            
        }
    }
}
