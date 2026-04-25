using Asp.Versioning;
using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper.Attributes;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Result.Application.DTOs.Events;
using JustGo.Result.Application.DTOs.ResultViewDtos;
using JustGo.Result.Application.Features.Events.Queries.GetEventDisciplinesList;
using JustGo.Result.Application.Features.Events.Queries.GetPlayerRankings;
using JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions;
using JustGo.Result.Application.Features.ResultView.Queries.GetEventDisciplines;
using JustGo.Result.Application.Features.ResultView.Queries.GetEvents;
using JustGo.Result.Application.Features.ResultView.Queries.GetFilterMetaData;
using JustGo.Result.Application.Features.ResultView.Queries.GetPlayerProfileMaxScore;
using JustGo.Result.Application.Features.ResultView.Queries.GetResults;
using JustGo.Result.Application.Features.ResultView.Queries.GetResultViewGridConfig;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JustGo.Result.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sports-results")]
[Tags("Results/View Sports Results")]
[TenantFromHeader]
public class SportsResultsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUtilityService _utilityService;

    public SportsResultsController(IMediator mediator, IUtilityService utilityService)
    {
        _mediator = mediator;
        _utilityService = utilityService;
    }

    [HttpPost("competitions")]
    public async Task<IActionResult> GetSportsCompetitionsAsync([FromBody] GetSportsCompetitionsQuery request,
        CancellationToken cancellationToken)
    {
        var sportTypeId = await _utilityService.GetTenantSportTypeAsync(cancellationToken);

        if (sportTypeId == 0)
        {
            return BadRequest(new ApiResponse<object, object>("Invalid SportTypeId", 400,
                "Sport Type ID is not configured for the tenant."));
        }

        request.SportType = (SportType)sportTypeId;

        var result = await _mediator.Send(request, cancellationToken);

        return FromResult(result, data => Ok(new ApiResponse<ResultCompetitionDto, object>(data)));
    }

    [HttpGet("events/{eventId}/disciplines")]
    public async Task<IActionResult> GetEventDisciplinesAsync([FromRoute] int eventId,
        CancellationToken cancellationToken)
    {
        var request = new GetEventDisciplinesQuery(eventId);

        var result = await _mediator.Send(request, cancellationToken);

        return FromResult(result, data => Ok(new ApiResponse<ICollection<EventDisciplineDto>, object>(data)));
    }

    [HttpPost("details")]
    public async Task<IActionResult> GetCompetitionDetailsAsync([FromBody] GetResultViewQuery request,
        CancellationToken cancellationToken)
    {
        var sportTypeId = await _utilityService.GetTenantSportTypeAsync(cancellationToken);

        if (sportTypeId == 0)
        {
            return BadRequest(new ApiResponse<object, object>("Invalid SportTypeId", 400,
                "Sport Type ID is not configured for the tenant."));
        }

        request.SportType = (SportType)sportTypeId;

        var result = await _mediator.Send(request, cancellationToken);
        return FromResult(result, data => Ok(new ApiResponse<object, object>(data)));
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEaEventsAsync([FromQuery] GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        var sportTypeId = await _utilityService.GetTenantSportTypeAsync(cancellationToken);

        if (sportTypeId == 0)
        {
            return BadRequest(new ApiResponse<object, object>("Invalid SportTypeId", 400,
                "Sport Type ID is not configured for the tenant."));
        }

        request.SportType = (SportType)sportTypeId;

        var result = await _mediator.Send(request, cancellationToken);
        return FromResult(result, data => Ok(new ApiResponse<GenericEventListResponse, object>(data)));
    }

    [HttpGet("grid-configurations/{competitionId:int}")]
    public async Task<IActionResult> GetGridConfigurationsAsync([FromRoute] int competitionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetResultViewGridConfigQuery(competitionId), cancellationToken);
        return FromResult(result, data => Ok(new ApiResponse<List<ResultViewGridColumnConfig>, object>(data)));
    }

    [HttpGet("get-event-disciplines")]
    public async Task<IActionResult> GetEventDisciplinesAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEventDisciplinesListQuery(), cancellationToken);
        return FromResult<List<SelectListItemDTO<string>>>(result,
            data => Ok(new ApiResponse<List<SelectListItemDTO<string>>, object>(data)));
    }

    [HttpGet("filter-metadata")]
    public async Task<IActionResult> GetFilterMetadataAsync([FromQuery, BindRequired] int competitionId,
        [FromQuery] int? roundId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFilterMetaDataQuery(competitionId, roundId), cancellationToken);
        return Ok(new ApiResponse<FilterMetadataDto, object>(result));
    }

    [HttpGet("get-Player-MaxScore")]
    public async Task<IActionResult> GetPlayerMaxScoreAsync(string memberId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return BadRequest(new ApiResponse<object, object>("InvalidMemberId", 400,
                "Member ID must be provided."));
        }

        var result = await _mediator.Send(new GetPlayerProfileMaxScoreQuery(memberId), cancellationToken);
        return FromResult(result, data => Ok(new ApiResponse<PlayerProfileMaxScoreDto, object>(data)));
    }

    [HttpGet("competition-metadata")]
    public async Task<IActionResult> GetCompetitionMetadata([FromQuery] GetCompetitionMetadataQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return FromResult<CompetitionCreateMetadataDto>(result, data => Ok(new ApiResponse<CompetitionCreateMetadataDto, object>(data)));
    }

}