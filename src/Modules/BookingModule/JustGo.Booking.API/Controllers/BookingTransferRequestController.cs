using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.DTOs.BookingTransferRequestDTOs;
using JustGo.Booking.Application.Features.BookingTransferRequest.Queries.CheckMemberPlanStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/transfer-request")]
    [ApiController]
    [Tags("Class Management")]
    public class BookingTransferRequestController : ControllerBase
    {
        private readonly IMediator _mediator;
        public BookingTransferRequestController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("check-member-plan-status")]
        [ProducesResponseType(typeof(ApiResponse<MemberPlanStatusResultDto, object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckMemberPlanStatus([FromBody] CheckMemberPlanStatusQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(new ApiResponse<MemberPlanStatusResultDto, object>(result));
        }
    }
}
