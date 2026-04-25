using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid;
using Azure.Core;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AuthModule.API.Middlewares
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        IMediator _mediator;
        private readonly IJweTokenService _jweTokenService;
        public TenantMiddleware(RequestDelegate next, IMediator mediator, IJweTokenService jweTokenService)
        {
            _next = next;
            _mediator = mediator;
            _jweTokenService = jweTokenService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string tenantGuid = string.Empty;
            var token = context?.Request.Headers["Authorization"].FirstOrDefault();
            if (token is not null)
            {
                //tenantGuid = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First(c => c.Type == "TenantGuid").Value;
                tenantGuid = _jweTokenService.GetClaimFromTokenByType(token, "TenantGuid");
                if (string.IsNullOrEmpty(tenantGuid))
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Tenant not found");
                    return;
                }

                var tenant = await _mediator.Send(new GetTenantByTenantGuidQuery(new Guid(tenantGuid)));
                if (tenant is not null)
                {
                    DatabaseSwitcher.UseTenantDatabase();
                }
            }
            await _next(context);
        }
   


    }
}
