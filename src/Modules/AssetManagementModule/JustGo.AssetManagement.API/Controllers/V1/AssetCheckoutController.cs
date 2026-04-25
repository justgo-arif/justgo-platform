using Asp.Versioning;
using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.IsExistsLeaseCartItem;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.ValidateProductPurchaseRule;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.API.Controllers.V1
{

    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-Checkout")]
    [ApiController]
    [Tags("Asset Management/Asset Checkout")]
    public class AssetCheckoutController: ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public AssetCheckoutController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize("asset_view_self", "view")]
        [HttpGet("check-cart-item")]
        public async Task<IActionResult> IsExistsCartItem(Guid entityId,int entityType, CancellationToken cancellationToken)
        {
            var query = new IsExistsLeaseCartItemQuery { LeaseId = entityId,EntityType = entityType };
            var exists = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<bool,object>(exists));
        }

        [CustomAuthorize("asset_view_self", "view")]
        [HttpGet("validate-purchase-rule")]
        public async Task<IActionResult> ValidatePurchaseRule(Guid assetRegisterId, Guid productId, CancellationToken cancellationToken)
        {
            var query = new ValidateProductPurchaseRuleQuery(productId, assetRegisterId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<AssetPurchaseRuleResultDTO, object>(result));
        }
    }
}
