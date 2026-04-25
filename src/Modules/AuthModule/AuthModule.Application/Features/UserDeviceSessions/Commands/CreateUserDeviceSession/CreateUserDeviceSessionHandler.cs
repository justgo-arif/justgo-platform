using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.UserDeviceSessions.Commands.CreateUserDeviceSession
{
    public class CreateUserDeviceSessionHandler : IRequestHandler<CreateUserDeviceSessionCommand, int>
    {
        private readonly LazyService<IWriteRepository<UserDeviceSessionInfo>> _writeRepository;

        public CreateUserDeviceSessionHandler(LazyService<IWriteRepository<UserDeviceSessionInfo>> writeRepository)
        {
            _writeRepository = writeRepository;
        }

        public async Task<int> Handle(CreateUserDeviceSessionCommand request, CancellationToken cancellationToken)
        {
            string sql = @"[dbo].[SaveUserDeviceSessionInfo]";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Id", request.Id, dbType: DbType.Int64);
            queryParameters.Add("@UserId", request.UserId, dbType: DbType.Int32);
            queryParameters.Add("@UserSyncId", request.UserSyncId, dbType: DbType.Guid);
            queryParameters.Add("@UserSessionId", request.UserSessionId);
            queryParameters.Add("@UserDeviceName", request.UserDeviceName);
            queryParameters.Add("@UserDeviceModel", request.UserDeviceModel);
            queryParameters.Add("@UserDeviceIP", request.UserDeviceIP);
            queryParameters.Add("@UserDevicePort", request.UserDevicePort);
            queryParameters.Add("@UserBrowserName", request.UserBrowserName);
            queryParameters.Add("@UserBrowserVersion", request.UserBrowserVersion);
            queryParameters.Add("@RefreshToken", request.RefreshToken);
            queryParameters.Add("@RefreshTokenExpiryMinutes", request.RefreshTokenExpiryMinutes, dbType: DbType.Int32);
            queryParameters.Add("@RefreshTokenExpiryDate", request.RefreshTokenExpiryDate, dbType: DbType.DateTime);
            queryParameters.Add("@UserLocation", request.UserLocation);
            return await _writeRepository.Value.ExecuteAsync(sql, cancellationToken, queryParameters);
        }
    }
}
