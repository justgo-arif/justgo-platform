using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetAssetMasterLicenses;
using JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetLicenseAdditionalFeeV2;
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

namespace JustGo.AssetManagement.API.Controllers.V2
{
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/asset-licenses")]
    [ApiController]
    [Tags("Asset Management/Asset Licenses")]
    public class AssetLicensesController: ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        public AssetLicensesController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpGet("metadata")]
        public async Task<IActionResult> GetAssetTypeMetadata(Guid assetRegisterId,Guid assetTypeId, int licenseTypeId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetMasterLicenseQuery(licenseTypeId, assetTypeId,assetRegisterId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("additional-fee")]
        public async Task<IActionResult> GetLicenseAdditionalFee([FromBody] GetLicenseAdditionalFeeQueryV2 request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}
