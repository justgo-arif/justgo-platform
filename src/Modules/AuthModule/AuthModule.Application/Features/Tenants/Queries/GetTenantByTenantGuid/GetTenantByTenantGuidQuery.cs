using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid
{
    public class GetTenantByTenantGuidQuery:IRequest<Tenant>
    {
        public Guid TenantGuid { get; set; }
        public GetTenantByTenantGuidQuery(Guid tenantGuid)
        {
            TenantGuid = tenantGuid;            
        }
    }
}
