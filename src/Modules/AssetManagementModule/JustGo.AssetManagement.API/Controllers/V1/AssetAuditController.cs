using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetMyClubs;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetAssetAudits;

namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/audits")]
    [ApiController]
    [Tags("Asset Management/Audits")]
    public class AssetAuditController: ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public AssetAuditController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }
        [CustomAuthorize("organisation_view", "view")]
        [HttpPost("list")]
        public async Task<IActionResult> GetList(GetAssetAuditsQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
