using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetCredentials.Commands.CreateCredential;
using JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetCredentialProduct;
using JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetCredentials;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.EditLicenses;
using JustGo.AssetManagement.Application.Features.AssetCredentials.Commands.EditCredentials;

namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-credentials")]
    [ApiController]
    [Tags("Asset Management/Asset Credentials")]
    public class AssetCredentialsController: ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

       public AssetCredentialsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpGet("credential/{assetRegisterId}")]
        public async Task<IActionResult> GetAssetCredentialsById(string assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetCredentialsQuery() { AssetRegisterId = assetRegisterId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_create_certification", "create")]
        [HttpPost("create-credential")]
        public async Task<IActionResult> CreateAssetCredential([FromBody] CreateAssetCredentialCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command , cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_edit_credential", "edit", "assetCredentialId")]
        [HttpPut("edit/{assetCredentialId}")]
        public async Task<IActionResult> EditAssetlicense(string assetCredentialId, [FromBody] EditCredentialCommand command, CancellationToken cancellationToken)
        {
            command.AssetCredentialId = assetCredentialId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize("asset_edit_basicDetail", "edit")]
        [HttpPatch("change-status/{assetCredentialId}")]
        public async Task<IActionResult> ChangeAssetStatus(string assetCredentialId, [FromBody] ChangeAssetCredentialStatusCommand command, CancellationToken cancellationToken)
        {
            command.AssetCredentialId = assetCredentialId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("credential-product/{credentialDocId}")]
        public async Task<IActionResult> GetCredentialProduct(int credentialDocId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetCredentialProductsQuery() { CredentialDocId = credentialDocId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpGet("asset-credential-permissions/{assetRegisterId}")]
        public async Task<IActionResult> GetAssetCredentialPermissions(string assetRegisterId, CancellationToken cancellationToken)
        {
            var resource = new Dictionary<string, object>()
            {
                { "assetRegisterId", assetRegisterId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("asset_allow_ui_credential_detail", cancellationToken, null, resource);
            return Ok(new ApiResponse<object, object>(null, permissions));
        }

    }
}
