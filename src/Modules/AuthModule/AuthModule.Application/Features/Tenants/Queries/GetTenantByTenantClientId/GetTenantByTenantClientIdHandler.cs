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

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantClientId
{
    public class GetTenantByTenantClientIdHandler : IRequestHandler<GetTenantByTenantClientIdQuery, Tenant>
    {       
        private readonly LazyService<IReadRepository<Tenant>> _readRepository;
        public GetTenantByTenantClientIdHandler(LazyService<IReadRepository<Tenant>> readRepository)
        {
            _readRepository = readRepository;            
        }

        public async Task<Tenant> Handle(GetTenantByTenantClientIdQuery request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT * FROM [dbo].[Tenants]
                               WHERE [TenantClientId]=@TenantClientId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantClientId", request.TenantClientId);
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
