using System.IdentityModel.Tokens.Jwt;
using Asp.Versioning;
using AuthModule.Application.Features.Account.Queries;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid;
using AuthModule.Application.Features.UserDeviceSessions.Queries.GetRefreshTokenExpiryDateByRefreshToken;
using AuthModule.Application.Features.Users.Commands.AuthenticateUser;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace AuthModule.API.Controllers.V2
{
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/accounts")]
    [ApiController]
    [Tags("Authentication/Accounts")]
    public class AccountsController : ControllerBase
    {
        IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtAthenticationService _jwtAthenticationService;
        private readonly IUtilityService _utilityService;
        public AccountsController(IMediator mediator, IHttpContextAccessor httpContextAccessor
            , IJwtAthenticationService jwtAthenticationService, IUtilityService utilityService)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
            _jwtAthenticationService = jwtAthenticationService;
            _utilityService = utilityService;
        }



        //[HttpPost("forget-password")]
        //public async Task<IActionResult> ForgetPasswordAsync(PasswordResetQuery resetQuery, CancellationToken cancellationToken)
        //{
        //    return Ok(new ApiResponse<object, object>(await _mediator.Send(resetQuery, cancellationToken)));
        //}


        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPasswordAsync(PasswordResetQuery resetQuery, CancellationToken cancellationToken)
        {
            var returnData = new OperationResult<bool>();
            returnData.Remark = "success";

            var result = await _mediator.Send(resetQuery, cancellationToken);
            returnData.Remark = result.Item1 ? "success" : "error";
            returnData.Data = result.Item1;
            returnData.StatusCode = 200;
            returnData.Message = result.Item2;
            return Ok(returnData);
        }


        [CustomAuthorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(command, cancellationToken)));
        }
    }
}
