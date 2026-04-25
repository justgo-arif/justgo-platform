using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetActionAssets;
using JustGo.AssetManagement.Application.Features.AssetReports.Commands.DownloadReport;
using JustGo.AssetManagement.Application.Features.AssetReports.Queries.GetAssetReports;
using JustGo.AssetManagement.Application.Features.Notes.Commands.DeleteNoteCommands;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-reports")]
    [ApiController]
    [Tags("Asset Management/Asset Reports")]
    public class AssetReportsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ICustomError _error;
        private readonly IMapper _mapper;

        public AssetReportsController(
            IMediator mediator,
            IAbacPolicyEvaluatorService abacPolicyEvaluatorService,
            ICustomError error,
            IMapper mapper)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _error = error;
            _mapper = mapper;
        }

        //[CustomAuthorize("asset_report_view", "view")]
        [CustomAuthorize("asset_view_detail", "view")]
        [HttpGet("")]
        public async Task<IActionResult> GetReports(string assetRegisterId,int entityType, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetReportsQuery() { AssetRegisterId = assetRegisterId,EntityType = entityType }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        //[CustomAuthorize("asset_view_detail", "view")]
        [CustomAuthorize("asset_view_detail", "view")]
        [HttpPost("Download/{entityId}")]
        public async Task<IActionResult> DownloadReport(string entityId,[FromBody] DownloadAssetReportQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request , cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}