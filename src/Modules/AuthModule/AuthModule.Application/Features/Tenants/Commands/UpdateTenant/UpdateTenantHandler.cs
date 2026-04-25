using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AuthModule.Application.Features.Tenants.Commands.UpdateTenant;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Tenants.Commands.UpdateTenant
{
    public class UpdateTenantHandler : IRequestHandler<UpdateTenantCommand, int>
    {
        private readonly LazyService<IWriteRepository<Tenant>> _writeRepository;

        public UpdateTenantHandler(LazyService<IWriteRepository<Tenant>> writeRepository)
        {
            _writeRepository = writeRepository;
        }

        public async Task<int> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();
            string sql = $@"UPDATE [dbo].[Tenants]
                               SET [TenantName] = @TenantName
                                  ,[TenantDescription] = @TenantDescription                                 
                                  ,[TenantClientId] = @TenantClientId
                                  ,[ApiUrl] = @ApiUrl
                                  ,[TenantDomainUrl] = @TenantDomainUrl
                                  ,[JwtAccessTokenSecretKey] = @JwtAccessTokenSecretKey
                                  ,[JwtAccessTokenExpiryMinutes] = @JwtAccessTokenExpiryMinutes
                                  ,[JwtRefreshTokenExpiryMinutes] = @JwtRefreshTokenExpiryMinutes
                             WHERE [Id]=@Id";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Id", request.Id, dbType: DbType.Int32);
            queryParameters.Add("@TenantName", request.TenantName);
            queryParameters.Add("@TenantDescription", request.TenantDescription);
            queryParameters.Add("@TenantClientId", request.TenantClientId);
            queryParameters.Add("@ApiUrl", request.TenantClientId);
            queryParameters.Add("@TenantDomainUrl", request.TenantDomainUrl);
            queryParameters.Add("@JwtAccessTokenSecretKey", request.JwtAccessTokenSecretKey);
            queryParameters.Add("@JwtAccessTokenExpiryMinutes", request.JwtAccessTokenExpiryMinutes, dbType: DbType.Int32);
            queryParameters.Add("@JwtRefreshTokenExpiryMinutes", request.JwtRefreshTokenExpiryMinutes, dbType: DbType.Int32);
            return await _writeRepository.Value.ExecuteAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
