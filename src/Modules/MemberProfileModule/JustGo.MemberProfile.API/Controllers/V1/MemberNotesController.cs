using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.Features.MemberNotes.Commands.ChangeStatusOfNotes;
using JustGo.MemberProfile.Application.Features.MemberNotes.Commands.SaveMemberNotes;
using JustGo.MemberProfile.Application.Features.MemberNotes.Queries.GetMemberNotes;
using JustGo.MemberProfile.Application.Features.MemberNotes.Queries.GetNotesCategory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/member_notes")]
    [ApiController]
    [Tags("Member Profile/Member Notes")]
    public class MemberNotesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MemberNotesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [CustomAuthorize]
        [HttpGet("note_categories")]
        public async Task<IActionResult> GetAllNoteCategories(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new NotesCategoryQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpPost("save_member_note")]
        public async Task<IActionResult> SaveMemberNote(SaveMemberNotesCommand request,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpPost("change_status_of_notes")]
        public async Task<IActionResult> ChangeStatusOfNotes(ChangeStatusOfNotesCommand request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpGet("member_notes")]
        public async Task<IActionResult> GetMemberNotes([FromQuery]GetMemberNotesQuery request,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}
