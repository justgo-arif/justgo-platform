using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantByDomain
{
    public class GetTenantByDomainHandler: IRequestHandler<GetTenantByDomainQuery, Tenant>
    {
        private readonly LazyService<IReadRepository<Tenant>> _readRepository;

        public GetTenantByDomainHandler(LazyService<IReadRepository<Tenant>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<Tenant> Handle(GetTenantByDomainQuery request, CancellationToken cancellationToken = default)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT TOP 1 * FROM [dbo].[Tenants]
                          WHERE [TenantDomainUrl] LIKE '%' + @TenantDomainUrl + '%'";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantDomainUrl", request.TenantDomain);
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
