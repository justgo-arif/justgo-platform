using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantClientId;
using AuthModule.Application.Features.Users.Queries.GetUserByLoginId;
using AuthModule.Domain.Entities;
using Dapper;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using AuthModule.Application.EmailServices;
using Microsoft.IdentityModel.Tokens;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Account.Queries
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
    {
        private readonly LazyService<IWriteRepository<string>> _writeRepository;
        private IMediator _mediator;
        private readonly IUtilityService _utilityService;
        private readonly ISystemSettingsService _systemSettingsService;
        public ChangePasswordCommandHandler(LazyService<IWriteRepository<string>> writeRepository,IMediator mediator
            , IUtilityService utilityService, ISystemSettingsService systemSettingsService)
        {
            _writeRepository = writeRepository;
            _mediator = mediator;
            _utilityService = utilityService;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            var setting = _systemSettingsService.GetSystemSettings("CORE.PASSWORD.MEMORY",cancellationToken);
            var user = await _mediator.Send(new GetUserByLoginIdQuery(request.UserName));

            if (Convert.ToInt32(setting) > 0 && user != null)
            {
                queryParameters.Add("@LoginId", user.LoginId);
                queryParameters.Add("@OldPass", _utilityService.Encrypt(user.Password));
                queryParameters.Add("@NewPass", _utilityService.Encrypt(request.NewPassword));
                queryParameters.Add("@HistoryCount", Convert.ToInt32(setting));

                var result = await _writeRepository.Value.ExecuteAsync("USER_CHANGE_PASSWORD", cancellationToken, queryParameters, null, "text");

                if (result >= 0)
                {
                    string sqlDelete = @"Delete from UserDeviceSessionInfo where UserId=@userId";
                    var queryParametersDelete = new DynamicParameters();
                    queryParametersDelete.Add("@userId", user.Userid);

                    await _writeRepository.Value.ExecuteAsync(sqlDelete, cancellationToken, queryParametersDelete, null, "text");
                }

                return result >= 0;
            }
            else return false;
        }
        
    }
}
