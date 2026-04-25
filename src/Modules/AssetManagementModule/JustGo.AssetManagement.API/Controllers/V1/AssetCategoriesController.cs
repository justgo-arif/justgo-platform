using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetCategories.Queries.GetAssetCategories;
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
    [Route("api/v{version:apiVersion}/asset-categories")]
    [ApiController]
    [Tags("Asset Management/Asset Categories")]
    public class AssetCategoriesController:ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public AssetCategoriesController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpGet("list/{assetTypeId}")]
        public async Task<IActionResult> GetAssetCategories(string assetTypeId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetCategoriesByTypeIdQuery() { AssetTypeId = assetTypeId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
