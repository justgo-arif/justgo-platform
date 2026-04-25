using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthModule.Application.Features.Tenants.Queries.GetTenants;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Queries.GetHierarchyTypes
{
    public class GetTenantsHandler : IRequestHandler<GetTenantsQuery, List<Tenant>>
    {
        private readonly LazyService<IReadRepository<Tenant>> _readRepository;
        public GetTenantsHandler(LazyService<IReadRepository<Tenant>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<Tenant>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = $@"SELECT *
                          FROM [dbo].[Tenants]";
            var queryParameters = new DynamicParameters();
            var tenants = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return tenants.AsList();
        }
    }
}
