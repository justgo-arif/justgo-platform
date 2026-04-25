using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetTypes;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-types")]
    [ApiController]
    [Tags("Asset Management/Asset Types")]
    public class AssetTypesController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public AssetTypesController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpGet("list")]
        public async Task<IActionResult> GetAssetTypes(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetTypesQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("details/{assetTypeId}")]
        public async Task<IActionResult> GetAssetType(Guid assetTypeId,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AssetMetadataQuery(assetTypeId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}
