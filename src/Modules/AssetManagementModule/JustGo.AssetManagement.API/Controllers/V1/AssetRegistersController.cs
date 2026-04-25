using Asp.Versioning;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseHistory;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetMyLeases;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.CompleteAssetSubmissionCommands;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.EditAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.RegisterAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetActionAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetCurrentAssetStep;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetCurrentAssetWarning;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetMyAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata;
using JustGo.AssetManagement.Application.Features.Notes.Commands.DeleteNoteCommands;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Infrastructure.CustomErrors;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pipelines.Sockets.Unofficial.Buffers;


namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-registers")]
    [ApiController]
    [Tags("Asset Management/Asset Registers")]
    public class AssetRegistersController:ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ICustomError _error;
        private readonly IMapper _mapper;
        public AssetRegistersController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService
            , ICustomError error, IMapper mapper)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _error = error;
            _mapper = mapper;
        }

        [CustomAuthorize("asset_create","create")]
        [HttpPost("create")]
        public async Task<IActionResult> RegistarAsset([FromBody] AssetRegisterCommand command,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            if (CustomResponse.IsFailure)
            {
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize("asset_edit_basicDetail", "edit", "assetRegisterId")]
        [HttpPut("save/{assetRegisterId}")]
        public async Task<IActionResult> EditAsset(string assetRegisterId, [FromBody] EditAssetCommand command, CancellationToken cancellationToken)
        {
            command.AssetRegisterId = assetRegisterId;
            var existingRecord = await _mediator.Send(new GetSingleAssetQuery() { AssetRegisterId = assetRegisterId }, cancellationToken);
            var mappedExistingRecord = _mapper.Map<EditAssetCommand>(existingRecord);
            var fieldMapping = await _mediator.Send(new AssetMetadataQuery(new Guid(command.TypeId)), cancellationToken);
            var modifiedFields = _abacPolicyEvaluatorService.GetModifiedFields(command, mappedExistingRecord);
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync("asset_basic_fields", cancellationToken);
            foreach (var field in modifiedFields)
            {
                var isPermitted = true;
                if (permissions is not null && permissions.TryGetValue(field, out var fieldPermission))
                {
                    isPermitted = fieldPermission.Edit;
                }
                if (!isPermitted)
                {
                    var displayName = field;                    
                    if (fieldMapping is not null)
                    {
                        var matchedField = fieldMapping.AssetTypeConfig.CoreFieldConfig.LabelConfig.FirstOrDefault(f => f.Field.Equals(field, StringComparison.OrdinalIgnoreCase));
                        if (matchedField is not null)
                        {
                            displayName = matchedField.Label;
                        }
                    }
                    _error.CustomValidation<object>($"You are not authorized to edit {displayName}");
                    return new EmptyResult();
                }
            }
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize("asset_edit_basicDetail", "edit", "assetRegisterId")]
        [HttpPatch("change-status/{assetRegisterId}")]
        public async Task<IActionResult> ChangeAssetStatus(string assetRegisterId, [FromBody] ChangeAssetStatusCommand command, CancellationToken cancellationToken)
        {
            command.AssetRegisterId = assetRegisterId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_view_list", "view")]
        [HttpPost("list")]
        public async Task<IActionResult> FilterAssets([FromBody] GetAdminAssetsQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_view_self", "view")]
        [HttpPost("my-list")]
        public async Task<IActionResult> MyList([FromBody] GetMyAssetsQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("action-required-list")]
        public async Task<IActionResult> ActionRequiredList([FromBody] GetActionAssetsQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_view_detail", "view")]
        [HttpGet("details/{assetRegisterId}")]
        public async Task<IActionResult> GetAsset(string assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSingleAssetQuery() { AssetRegisterId = assetRegisterId }, cancellationToken);
            var resource = new Dictionary<string, object>()
            {
                { "assetRegisterId", assetRegisterId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluateCombinedPoliciesAsync(
                               new[] { "asset_allow_ui_detail", "asset_basic_fields" },
                               new[] { "ui", "fields" },
                               cancellationToken,
                               null,
                               resource);

            return Ok(new ApiResponse<object, object>(result, permissions));
        }

        [CustomAuthorize("asset_view_detail", "view")]
        [HttpGet("notifications/{assetRegisterId}")]
        public async Task<IActionResult> GetAssetWarnings(string assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCurrentAssetWarningQuery() { AssetRegisterId = assetRegisterId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_view_detail", "view")]
        [HttpGet("journey-completion-steps/{assetRegisterId}")]
        public async Task<IActionResult> GetCurrentStep(string assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCurrentAssetStepQuery() { AssetRegisterId = assetRegisterId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_delete_basicDetail", "delete")]
        [HttpDelete("delete/{assetRegisterId}")]
        public async Task<IActionResult> DeleteAsset(string assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteAssetCommand() { AssetRegisterId = assetRegisterId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize("asset_view_detail", "view")]
        [HttpPatch("complete-submission/{assetRegisterId}")]
        public async Task<IActionResult> CompleteAssetSubmission(string assetRegisterId, [FromBody] CompleteAssetSubmissionCommand command, CancellationToken cancellationToken)
        {
            command.AssetRegisterId = assetRegisterId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }



        [CustomAuthorize("asset_edit_basicDetail", "edit")]
        [HttpPatch("reinstate/{assetRegisterId}")]
        public async Task<IActionResult> Reinstate(string assetRegisterId, [FromBody] AssetReinstateCommand command, CancellationToken cancellationToken)
        {
            command.AssetRegisterId = assetRegisterId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_view_detail", "view")]
        [HttpPost("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate(GetDuplicateAssetQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result != null));
        }

    }
}
