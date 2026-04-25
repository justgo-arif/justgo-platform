using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using AuthModule.Application.Features.Tenants.Commands.CreateTenant;
using AuthModule.Application.Features.Tenants.Commands.DeleteTenant;
using AuthModule.Application.Features.Tenants.Commands.UpdateTenant;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByDomain;
using AuthModule.Application.Features.Tenants.Queries.GetTenantById;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid;
using AuthModule.Application.Features.Tenants.Queries.GetTenantGuidByDomain;
using AuthModule.Application.Features.Tenants.Queries.GetTenants;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/tenants")]
    [ApiController]
    [Tags("Authentication/Tenants")]
    public class TenantsController : ControllerBase
    {
        IMediator _mediator;
        public TenantsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [MapToApiVersion("1.0")]
        [HttpGet]
        public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTenantsQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTenantById(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpGet("by-guid/{guid}")]
        public async Task<IActionResult> GetTenantByTenantGuid(string guid, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTenantByTenantGuidQuery(new Guid(guid)), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpGet("by-domain")]
        public async Task<IActionResult> GetTenantByDomain(CancellationToken cancellationToken)
        {
            Request.Headers.TryGetValue("Origin", out var tenantDomain);
            var result = await _mediator.Send(new GetTenantByDomainQuery(tenantDomain), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpGet("guid-by-domain")]
        public async Task<IActionResult> GetTenantGuidByDomain(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTenantGuidByDomainQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateTenantCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateTenantCommand command, CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [MapToApiVersion("1.0")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var command=new DeleteTenantCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
