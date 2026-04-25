using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubDetails;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubsByAssetLeaseId;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubsByAssetModuleEntityId;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetMyClubs;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetMyClubsByAssetId;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenses;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/clubs")]
    [ApiController]
    [Tags("Asset Management/Clubs")]
    public class ClubsController: ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public ClubsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }
        [CustomAuthorize("organisation_view", "view")]
        [HttpGet("my-clubs")]
        public async Task<IActionResult> GetMyClubs(Guid memberId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyClubsQuery(memberId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize("organisation_view", "view")]
        [HttpGet("asset-owner-clubs/{assetRegisterId}")]
        public async Task<IActionResult> GetMyClubsByAssetId(Guid assetRegisterId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyClubsByAssetIdQuery(assetRegisterId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("organisation_view", "view")]
        [HttpGet("lease-owner-clubs/{assetLeaseId}")]
        public async Task<IActionResult> GetClubsByAssetLeaseId(string assetLeaseId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetClubsByAssetLeaseIdQuery(assetLeaseId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("organisation_view", "view")]
        [HttpGet("list/{entityType}/{entityId}")]
        public async Task<IActionResult> GetClubsByEntityId(int entityType, string entityId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetClubsByAssetModuleQuery((EntityType)entityType, entityId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("organisation_view", "view")]
        [HttpGet("details")]
        public async Task<IActionResult> GetDetails(string ClubId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetClubDetailsQuery()
            {
                ClubId = ClubId
            }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
