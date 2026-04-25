using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid
{
    public class GetTenantByTenantGuidHandler : IRequestHandler<GetTenantByTenantGuidQuery, Tenant>
    {
        private readonly LazyService<IReadRepository<Tenant>> _readRepository;
        public GetTenantByTenantGuidHandler(LazyService<IReadRepository<Tenant>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<Tenant> Handle(GetTenantByTenantGuidQuery request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = $@"SELECT *
                          FROM [dbo].[Tenants]
                          WHERE [TenantGuid]=@TenantGuid";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantGuid", request.TenantGuid, dbType: DbType.Guid);
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
