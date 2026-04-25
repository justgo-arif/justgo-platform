using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.CancelLicenses;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.CreateLicense;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.DeleteLicense;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.EditLicenses;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetCartAssetLicensesQuery;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Commands.RemoveAssetCartItems;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.AssetLicenseDefination;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetCartItems;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenseAdditionalFee;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenses;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetUpgradeLicenses;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-licenses")]
    [ApiController]
    [Tags("Asset Management/Asset Licenses")]
    public class AssetLicensesController:ControllerBase
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
        public async Task<IActionResult> GetAssetTypeMetadata(Guid assetTypeId,int licenseTypeId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMasterLicenses(licenseTypeId,assetTypeId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_create_license", "create", "assetRegisterId")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateAssetlicense([FromBody] CreateAssetLicenseCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize("asset_edit_license", "edit")]
        [HttpPut("edit/{assetLicenseId}")]
        public async Task<IActionResult> EditAssetlicense(string assetLicenseId, [FromBody] EditLicenseCommand command, CancellationToken cancellationToken)
        {
            command.AssetLicenseId = assetLicenseId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_cancel_license", "cancel")]
        [HttpPut("cancel/{assetLicenseId}")]
        public async Task<IActionResult> CancelAssetlicense(string assetLicenseId, [FromBody] CancelLicenseCommand command, CancellationToken cancellationToken)
        {
            command.AssetLicenseId = assetLicenseId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("additional-fee")]
        public async Task<IActionResult> GetLicenseAdditionalFee([FromBody] GetLicenseAdditionalFeeQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_delete_license", "delete")]
        [HttpDelete("delete/{AssetLicenseId}")]
        public async Task<IActionResult> DeleteAssetLicense(string AssetLicenseId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteAssetLicenseCommand() { AssetLicenseId = AssetLicenseId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [HttpGet("upgrade-metadata")]
        public async Task<IActionResult> GetAssetTypeLicenseUpgradeMetadata(Guid assetTypeId, int licenseTypeId,Guid licenseId, Guid assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUpgradeLicenseQuery(licenseTypeId, assetTypeId,licenseId,assetRegisterId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [HttpGet("cart-validate")]
        public async Task<IActionResult> GetShoppingCartItems(Guid assetRegisterId, Guid productId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RemoveAssetCartItemsCommand(assetRegisterId,productId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_edit_basicDetail", "edit")]
        [HttpPatch("change-status/{assetLicenseId}")]
        public async Task<IActionResult> ChangeAssetStatus(string assetLicenseId, [FromBody] ChangeAssetLicenseStatusCommand command, CancellationToken cancellationToken)
        {
            command.AssetLicenseId = assetLicenseId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("purchasable-items/{assetRegisterId}")]
        public async Task<IActionResult> GetAssetTypeLicenseUpgradeMetadata(Guid assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCartAssetLicensesQuery(assetRegisterId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("defination/{assetTypeId}")]
        public async Task<IActionResult> AssetLicenseDefination(Guid assetTypeId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AssetLicenseDefinationQuery(assetTypeId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("asset-license-permissions/{assetRegisterId}")]
        public async Task<IActionResult> GetAssetLicensePermissions(string assetRegisterId, CancellationToken cancellationToken)
        {
            var resource = new Dictionary<string, object>()
            {
                { "assetRegisterId", assetRegisterId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("asset_allow_ui_license_detail", cancellationToken, null, resource);
            return Ok(new ApiResponse<object, object>(null, permissions));
        }

    }
}
