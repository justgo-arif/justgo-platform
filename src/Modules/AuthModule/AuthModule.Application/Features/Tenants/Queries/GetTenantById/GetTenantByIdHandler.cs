using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Queries.GetTenantById
{
    public class GetTenantByIdHandler : IRequestHandler<GetTenantByIdQuery, Tenant>
    {
        private readonly LazyService<IReadRepository<Tenant>> _readRepository;
        public GetTenantByIdHandler(LazyService<IReadRepository<Tenant>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<Tenant> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = @"SELECT *
                          FROM [dbo].[Tenants]
                          WHERE [Id]=@Id";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Id", request.Id, dbType: DbType.Int32);
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
