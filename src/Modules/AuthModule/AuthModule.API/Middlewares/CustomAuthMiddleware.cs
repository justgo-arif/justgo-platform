using System.IdentityModel.Tokens.Jwt;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AuthModule.API.Middlewares
{
    public class CustomAuthMiddleware
    {
        private readonly RequestDelegate _next;
        IMediator _mediator;
        private readonly IJwtAthenticationService _jwtAthenticationService;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly IJweTokenService _jweTokenService;
        public CustomAuthMiddleware(RequestDelegate next, IMediator mediator
            , IJwtAthenticationService jwtAthenticationService
            , IAbacPolicyEvaluatorService abacPolicyEvaluatorService
            , IJweTokenService jweTokenService)
        {
            _next = next;
            _mediator = mediator;
            _jwtAthenticationService = jwtAthenticationService;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _jweTokenService = jweTokenService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (context.GetEndpoint()?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                if (token is null)
                {
                    await _next(context);
                    return;
                }
                if(context.Request.Path.Value.ToLowerInvariant().Contains("refresh-token"))
                {
                    await _next(context);
                    return;
                }
            }
            
            if (token is not null)
            {
                var cancellationToken = context.RequestAborted;
                //var tenantGuid = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "TenantGuid").Value;
                var tenantGuid = _jweTokenService.GetClaimFromTokenByType(token, "TenantGuid");
                if (tenantGuid is not null)
                {
                    var tenant = await _mediator.Send(new GetTenantByTenantGuidQuery(new Guid(tenantGuid)), cancellationToken);
                    if (tenant is not null)
                    {
                        //Authentication
                        _jwtAthenticationService.AttachUserToContext(context, token, tenant);

                        //Authorization
                        context.Items["isAuthorized"] = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(cancellationToken);
                    }
                }
            }
          
            await _next(context);
        }



    }
}
