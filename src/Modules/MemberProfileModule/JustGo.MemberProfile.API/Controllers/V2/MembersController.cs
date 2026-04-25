using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberEmergencyContactBySyncGuid;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V2
{
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/members")]
    [ApiController]
    [Tags("Member Profile/Members")]
    public class MembersController : ControllerBase
    {
        IMediator _mediator;

        public MembersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [CustomAuthorize("member_view_detail", "view", "memberId")]
        [MapToApiVersion("2.0")]
        [HttpGet("emergency-contact/{memberId}")]
        public async Task<IActionResult> GetMemberEmergencyContactBySyncGuid(string memberId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMemberEmergencyContactBySyncGuidQuery(new Guid(memberId)), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}
