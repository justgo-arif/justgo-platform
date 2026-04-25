using Asp.Versioning;
using AuthModule.Application.Features.Notes.Commands.CreateNotes;
using AuthModule.Application.Features.Notes.Commands.DeleteNotes;
using AuthModule.Application.Features.Notes.Commands.EditNotes;
using AuthModule.Application.Features.Notes.Queries.GetNotes;
using AuthModule.Application.Features.Notes.Queries.GetNotesWithPaginations;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static JustGo.Authentication.Infrastructure.Logging.AuditScheme.EmailManagement;

namespace AuthModule.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/notes")]
    [ApiController]
    [Tags("Notes/Notes")]
    public class NotesController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public NotesController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpGet("list/{entityType}/{entityId}/{module}")]
        public async Task<IActionResult> GetNotes(int entityType, Guid entityId, string module, CancellationToken cancellationToken)
        {
            var policyName = string.Empty;
            var resource = new Dictionary<string, object>();
            if (module.ToLowerInvariant().Equals("member"))
            {
                policyName = "member_profile_view_note";
                resource = new Dictionary<string, object>()
                {
                    { "id", entityId.ToString() ?? string.Empty }
                };
            }
            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "view", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var result = await _mediator.Send(new GetNotesQuery(entityType, entityId, module), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpGet("list-offset")]
        public async Task<IActionResult> GetNotes([FromQuery] GetNotesWithPaginationsQuery request, CancellationToken cancellationToken)
        {
            var policyName = string.Empty;
            var resource = new Dictionary<string, object>();
            if (request.Module.ToLowerInvariant().Equals("member"))
            {
                policyName = "member_profile_view_note";
                resource = new Dictionary<string, object>()
                {
                    { "id", request.EntityId.ToString() ?? string.Empty }
                };
            }
            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "view", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpGet("list-keyset")]
        public async Task<IActionResult> GetNotes([FromQuery] GetNotesWithPaginationsKeysetQuery request, CancellationToken cancellationToken)
        {
            var policyName = string.Empty;
            var resource = new Dictionary<string, object>();
            if (request.Module.ToLowerInvariant().Equals("member"))
            {
                policyName = "member_profile_view_note";
                resource = new Dictionary<string, object>()
                {
                    { "id", request.EntityId.ToString() ?? string.Empty }
                };
            }
            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "view", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("add")]
        public async Task<IActionResult> AddNote(CreateNotesCommand command, CancellationToken cancellationToken)
        {
            var policyName = string.Empty;
            var resource = new Dictionary<string, object>();
            if (command.Module.ToLowerInvariant().Equals("member"))
            {
                policyName = "member_profile_add_note";
                resource = new Dictionary<string, object>()
                {
                    { "id", command.EntityId.ToString() ?? string.Empty }
                };
            }
            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "add", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPut("save/{id}")]
        public async Task<IActionResult> EditNote(Guid id, EditNotesCommand command, CancellationToken cancellationToken)
        {
            var policyName = string.Empty;
            var resource = new Dictionary<string, object>();
            if (command.Module.ToLowerInvariant().Equals("member"))
            {
                policyName = "member_profile_edit_note";
                resource = new Dictionary<string, object>()
                {
                    { "id", command.EntityId.ToString() ?? string.Empty }
                };
            }
            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "edit", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            command.NotesGuid = id;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpDelete("delete/{id}/{module}")]
        public async Task<IActionResult> DeleteNotes(Guid id, string module, CancellationToken cancellationToken)
        {
            //Have some limitation to implement abac for this API because we only have NotesGuid and Module, we don't have EntityId to evaluate policy, so we will evaluate policy based on NotesGuid and Module, but it will cause some problem in the future when we want to use this API for other module that not have EntityId in the resource, so we need to find a way to get EntityId from NotesGuid and Module, or we can just skip abac for this API and only check if the user have permission to delete note or not, but it will cause some security issue because we don't know if the user have permission to delete note for specific entity or not, so we need to find a way to get EntityId from NotesGuid and Module to evaluate policy correctly.
            //var policyName = string.Empty;
            //var resource = new Dictionary<string, object>();
            //if (module.ToLowerInvariant().Equals("member"))
            //{
            //    policyName = "member_profile_delete_note";
            //    resource = new Dictionary<string, object>()
            //    {
            //        { "id", userId.ToString() ?? string.Empty }
            //    };
            //}
            //var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "delete", resource, cancellationToken);
            //if (!isPermitted)
            //{
            //    throw new ForbiddenAccessException();
            //}

            var result = await _mediator.Send(new DeleteNotesCommand(id, module), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }



    }
}
