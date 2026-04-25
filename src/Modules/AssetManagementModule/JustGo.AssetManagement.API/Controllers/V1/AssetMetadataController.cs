using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.AssetAdditionalFeeByType;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetAdditionalFormMetadata;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetStatusMetaData;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetTagsMetadata;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetClubsMetadata;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetCredentialMetaDatas;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetMembersMetadata;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetReasonsMetaData;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-metadata")]
    [ApiController]
    [Tags("Asset Management/Asset Metadata")]
    public class AssetMetadataController:ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        public AssetMetadataController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpGet("credentials/{assetTypeId}")]
        public async Task<IActionResult> GetCredentialMetadata(string assetTypeId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCredentialMetaDataQuery() { AssetTypeId = assetTypeId }, cancellationToken);            
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("tags")]
        public async Task<IActionResult> GetTagsMetadata(GetAssetTagsMetaDataQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("statuses")]
        public async Task<IActionResult> GetStatusMetadata(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetStatusMetaDataQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [HttpGet("lease-statuses")]
        public async Task<IActionResult> GetLeaseStatusMetadata(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLeaseStatusMetaDataQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("organisation_view", "view")]
        [HttpPost("clubs")]
        public async Task<IActionResult> GetClubsMetadata(GetClubsMetadataQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize()]
        [HttpPost("members")]
        public async Task<IActionResult> GetMembersMetadata(GetMembersMetadataQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("member_view", "view")]
        [HttpPost("my-list-members")]
        public async Task<IActionResult> GetMyListMembersMetadata(GetMyListMembersMetadataQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("action-reasons")]
        public async Task<IActionResult> GetActionReasonsMetadata(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetReasonsMetaDataQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize("asset_detail_metadata", "view")]
        [HttpPost("asset-details")]
        public async Task<IActionResult> GetAssetDetailsMetaData(GetAssetDetailsMetaDataQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("additional-form/{assetTypeId}")]
        public async Task<IActionResult> GetAssetAdditionalFormMetaData(string assetTypeId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetAdditionalFormMetadataQuery() { AssetTypeId = assetTypeId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("additional-fee/{entityType}/{ownerId}")]
        public async Task<IActionResult> GetAssetFee(int entityType, string entityId, string ownerId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send( new GetAssetAdditionalFeeByTypeQuery((EntityType)entityType, entityId, ownerId),cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}
