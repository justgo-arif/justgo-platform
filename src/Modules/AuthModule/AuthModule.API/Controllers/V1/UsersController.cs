using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using AuthModule.Application;
using AuthModule.Application.Features.Users.Commands.CreateUser;
using AuthModule.Application.Features.Users.Queries.GetUserByLoginId;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    [ApiController]
    [Tags("Authentication/Users")]
    public class UsersController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        public UsersController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize("allowUserProfileInfo", "view", "loginId")]
        [MapToApiVersion("1.0")]
        [HttpGet("user-by-login-id")]
        public async Task<IActionResult> GetUserByLoginId(string loginId, CancellationToken cancellationToken)
        {
            //var resource = new { loginId = loginId };
            //if (!_abacPolicyEvaluatorService.EvaluatePolicy("allowUserProfileInfo", "view", resource))
            //{
            //    return Ok("No permission");
            //}
            var result = await _mediator.Send(new GetUserByLoginIdQuery(loginId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpGet("user-by-guid")]
        public async Task<IActionResult> GetUserByUserSyncId(string userSyncId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserByUserSyncIdQuery(new Guid(userSyncId)), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("allowCredentialAdd", "add")]
        [MapToApiVersion("1.0")]
        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("allowCredentialAdd2", "add", "age", "location", "time", "IP")]
        [MapToApiVersion("1.0")]
        [HttpPost("create2")]
        public async Task<IActionResult> Create2(CreateUserCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("allowCredentialAdd3", "add")]
        [MapToApiVersion("1.0")]
        [HttpPost("create3")]
        public async Task<IActionResult> Create3(CreateUserCommand command)
        {
            //use foreach loop here
            var resource = new string[] { "Title", "PostCode" };
            //if (!_abacPolicyEvaluatorService.EvaluatePolicy("allowUserProfileInfoFieldAdd", "add", resource))
            //{
            //    command.Title = null;
            //    command.PostCode = null;
            //}
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize("allowCredentialEdit", "edit")]
        [MapToApiVersion("1.0")]
        [HttpPut("update")]
        public async Task<IActionResult> Update(CreateUserCommand command)
        {
            //use foreach loop here
            var OldUser = await _mediator.Send(new GetUserByLoginIdQuery(command.LoginId));
            var resource = new string[] { "Title", "PostCode" };
            //if (!_abacPolicyEvaluatorService.EvaluatePolicy("allowUserProfileInfoFieldEdit", "add", resource))
            //{
            //    command.Title = OldUser.Title;
            //    command.PostCode = OldUser.PostCode;
            //}
            var result = await _mediator.Send(command);
            return Ok(new ApiResponse<object, object>(result));
        }






    }
}
