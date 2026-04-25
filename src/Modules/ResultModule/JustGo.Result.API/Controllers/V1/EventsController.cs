using Asp.Versioning;
using AuthModule.Application.DTOs.Lookup;
using AuthModule.Application.Features.Lookup.Queries.GetCountys;
using AuthModule.Application.Features.Lookup.Queries.GetGender;
using JustGo.Authentication.Helper.Attributes;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using JustGo.Result.Application.Features.Events.Commands.AddCompetition;
using JustGo.Result.Application.Features.Events.Commands.AddEventCompetition;
using JustGo.Result.Application.Features.Events.Commands.DeleteEventCompetition;
using JustGo.Result.Application.Features.Events.Commands.UpdateCompetition;
using JustGo.Result.Application.Features.Events.Commands.UpdateEventCompetition;
using JustGo.Result.Application.Features.Events.Commands.UpdateResultCompetitionRanking;
using JustGo.Result.Application.Features.Events.Queries.GetCompetition;
using JustGo.Result.Application.Features.Events.Queries.GetEventCategorys;
using JustGo.Result.Application.Features.Events.Queries.GetEventCompetitions;
using JustGo.Result.Application.Features.Events.Queries.GetEventList;
using JustGo.Result.Application.Features.Events.Queries.GetEventPlayers;
using JustGo.Result.Application.Features.Events.Queries.GetEventType;
using JustGo.Result.Application.Features.Events.Queries.GetEventYears;
using JustGo.Result.Application.Features.Events.Queries.GetPlayerEventsHistory;
using JustGo.Result.Application.Features.Events.Queries.GetPlayerMatchHistory;
using JustGo.Result.Application.Features.Events.Queries.GetPlayerProfile;
using JustGo.Result.Application.Features.Events.Queries.GetPlayerRankings;
using JustGo.Result.Application.Features.Events.Queries.GetSportsName;
using JustGo.Result.Application.Features.ResultView.Queries.GetPlayerPerformanceGlobalStats;
using JustGo.Result.Application.Features.ResultView.Queries.GetPlayerPerformanceYearlyStats;
using JustGo.Result.Application.Features.ResultView.Queries.GetTournamentRatingOverTime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Result.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/events-results")]
    [Tags("Results/Events")]
    [TenantFromHeader]
    public class EventsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public EventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-events")]
        public async Task<IActionResult> GetEventsAsync([FromQuery] GetEventListQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<EventListResponse, object>(data)));
        }

        [HttpGet("get-competitions-matches")]
        public async Task<IActionResult> GetEventCompetitionsAsync([FromQuery] GetEventCompetitionsQuery query, CancellationToken cancellationToken)
        {
            if (query.EventId <= 0)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidEventId", 400,
                    "Event ID must be greater than 0."));
            }

            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<EventCompetitionResponse, object>(data)));
        }

        [HttpGet("get-players")]
        public async Task<IActionResult> GetEventPlayersAsync([FromQuery] GetEventPlayersQuery query, CancellationToken cancellationToken)
        {
            if (query.EventId <= 0)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidEventId", 400,
                    "Event ID must be greater than 0."));
            }

            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<EventPlayersResponse, object>(data)));
        }

        [HttpGet("get-player-profile")]
        public async Task<IActionResult> GetPlayerProfileAsync([FromQuery] GetPlayerProfileQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<PlayerProfileDto, object>(data)));
        }

        [HttpGet("rankings")]
        public async Task<IActionResult> GetPlayerRankingsAsync([FromQuery] GetPlayerRankingsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<PlayerRankingsResponse, object>(data)));
        }

        [HttpGet("get-event-categorys")]
        public async Task<IActionResult> GetEventCategorysAsync([FromQuery] string? resultEventTypeId, CancellationToken cancellationToken)
        {
            var query = new GetEventCategorysQuery(resultEventTypeId);
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult<List<SelectListItemDTO<string>>>(result, data => Ok(new ApiResponse<List<SelectListItemDTO<string>>, object>(data)));
        }

        [HttpGet("get-player-match-history")]
        public async Task<IActionResult> GetPlayerMatchHistoryAsync([FromQuery] GetPlayerMatchHistoryQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(query.PlayerId))
            {
                return BadRequest(new ApiResponse<object, object>("InvalidPlayerId", 400,
                    "Player ID must be needed."));
            }

            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<PlayerMatchHistoryResponse, object>(data)));
        }

        [HttpGet("sports-name")]
        public async Task<IActionResult> GetSportsNameAsync(CancellationToken cancellationToken)
        {
            var query = new GetSportsNameQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
        }

        [HttpGet("get-event-years")]
        public async Task<IActionResult> GetEventYearsAsync(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEventYearsQuery(), cancellationToken);
            return FromResult<List<SelectListItemDTO<string>>>(result, data => Ok(new ApiResponse<List<SelectListItemDTO<string>>, object>(data)));
        }

        [CustomAuthorize]
        [HttpPost("add-competition-match")]
        public async Task<IActionResult> AddEventCompetitionAsync([FromBody] AddEventCompetitionCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidInput", 400,
                    "Invalid input data. Please check all required fields."));
            }

            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<AddEventCompetitionResponse, object>(data)));
        }

        [CustomAuthorize]
        [HttpDelete("delete-competition-match/{matchId}")]
        public async Task<IActionResult> DeleteEventCompetitionAsync(int matchId, CancellationToken cancellationToken)
        {
            if (matchId <= 0)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidMatchId", 400,
                    "Match ID must be greater than 0."));
            }

            var command = new DeleteEventCompetitionCommand
            {
                MatchId = matchId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<DeleteEventCompetitionResponse, object>(data)));
        }

        [CustomAuthorize]
        [HttpPut("update-competition-match")]
        public async Task<IActionResult> UpdateEventCompetitionAsync([FromBody] UpdateEventCompetitionCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidInput", 400,
                    "Invalid input data. Please check all required fields."));
            }

            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<UpdateEventCompetitionResponse, object>(data)));
        }

        [HttpGet("get-player-events-history")]
        public async Task<IActionResult> GetPlayerEventsHistoryAsync([FromQuery] GetPlayerEventsHistoryQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(query.PlayerId))
            {
                return BadRequest(new ApiResponse<object, object>("InvalidPlayerId", 400,
                    "Player ID must be needed."));
            }

            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<PlayerEventsHistoryResponse, object>(data)));
        }

        [HttpGet("get-countys")]
        public async Task<IActionResult> GetCountys(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCountysQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [HttpGet("get-gender")]
        public async Task<IActionResult> GetGender(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetGenderQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [HttpGet("get-Event-Types")]
        public async Task<IActionResult> GetEventTypesAsync([FromQuery] GetEventTypeQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult<List<EventTypeResponse>>(result, data => Ok(new ApiResponse<List<EventTypeResponse>, object>(data)));
        }

        [CustomAuthorize]
        [HttpPut("update-result-ranking")]
        public async Task<IActionResult> UpdateResultCompetitionRankingAsync([FromBody] UpdateResultCompetitionRankingCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidInput", 400,
                    "Invalid input data. Please check all required fields."));
            }

            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<UpdateResultCompetitionRankingResponse, object>(data)));
        }

        [HttpPost("add-competition")]
        public async Task<IActionResult> AddCompetitionAsync([FromBody] AddCompetitionCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidInput", 400,
                    "Invalid input data. Please check all required fields."));
            }

            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<AddCompetitionResponse, object>(data)));
        }

        [CustomAuthorize]
        [HttpPut("update-competition")]
        public async Task<IActionResult> UpdateCompetitionAsync([FromBody] UpdateCompetitionCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidInput", 400,
                    "Invalid input data. Please check all required fields."));
            }

            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<UpdateCompetitionResponse, object>(data)));
        }

        [HttpGet("get-player-performance-global-stats")]
        public async Task<IActionResult> GetPlayerPerformanceGlobalStats([FromQuery] GetPlayerPerformanceGlobalStatsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<PlayerPerformanceGlobalStatsResponse, object>(data)));
        }

        [HttpGet("get-player-performance-yearly-stats")]
        public async Task<IActionResult> GetPlayerPerformanceYearlyStats([FromQuery] GetPlayerPerformanceYearlyStatsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<PlayerPerformanceYearlyStatsResponse, object>(data)));
        }

        [HttpGet("get-tournament-rating-over-time")]
        public async Task<IActionResult> GetTournamentRatingOverTime([FromQuery] GetTournamentRatingOverTimeQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<List<TournamentRatingOverTimeResponse>, object>(data)));
        }

        [HttpGet("get-competition/{eventId:int:required}")]
        public async Task<IActionResult> GetCompetitionAsync([FromRoute] int eventId, CancellationToken cancellationToken)
        {
            if (eventId <= 0)
            {
                return BadRequest(new ApiResponse<object, object>("InvalidEventId", 400,
                    "Event ID must be greater than 0."));
            }

            var query = new GetCompetitionQuery { EventId = eventId };
            var result = await _mediator.Send(query, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<GetCompetitionResponse, object>(data)));
        }
    }
}
