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

namespace AuthModule.Application.Features.Tenants.Commands.DeleteTenant
{
    public class DeleteTenantHandler : IRequestHandler<DeleteTenantCommand, int>
    {
        private readonly LazyService<IWriteRepository<Tenant>> _writeRepository;

        public DeleteTenantHandler(LazyService<IWriteRepository<Tenant>> writeRepository)
        {
            _writeRepository = writeRepository;
        }
        public async Task<int> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = "DELETE FROM [dbo].[Tenants] WHERE [Id]=@Id";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Id", request.Id, dbType: DbType.Int32);
            return await _writeRepository.Value.ExecuteAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
