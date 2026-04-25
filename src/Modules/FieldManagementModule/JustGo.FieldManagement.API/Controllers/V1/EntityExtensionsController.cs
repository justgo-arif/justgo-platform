using Asp.Versioning;
using JustGo.Authentication.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.FieldManagement.Application.DTOs;
using JustGo.FieldManagement.Application.Features.EntityData.Commands.CreateEntityData;
using JustGo.FieldManagement.Application.Features.EntityData.Commands.CreateSpecificEntityData;
using JustGo.FieldManagement.Application.Features.EntityData.Commands.SaveWebletPreference;
using JustGo.FieldManagement.Application.Features.EntityData.Queries.GetEntityData;
using JustGo.FieldManagement.Application.Features.EntityData.Queries.GetFieldNameByFieldId;
using JustGo.FieldManagement.Application.Features.EntityData.Queries.GetItemIdBySyncGuid;
using JustGo.FieldManagement.Application.Features.EntityData.Queries.GetSpecificEntityData;
using JustGo.FieldManagement.Application.Features.EntityData.Queries.GetWebletPreference;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.CreateEntityExtensionFieldsetAttachments;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.CreateEntitySchema;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.DeleteEntityExtensionForm;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionAttachments;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionSchemaById;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUi;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiNew;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiSchemaById;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntitySchema;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetMenuOrganisation;
using JustGo.FieldManagement.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.FieldManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/entity-extensions")]
    [ApiController]
    [Tags("Field Management/EntityExtensions")]
    public class EntityExtensionsController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ICustomError _error;
        public EntityExtensionsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService
            , ICustomError error)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _error = error;
        }

        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("ui-tab-items/{ownerType}/{ownerId}/{extensionArea}/{extensionEntityId}")]
        public async Task<IActionResult> GetEntityExtensionUi(string ownerType, int ownerId, string extensionArea, int extensionEntityId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntityExtensionUiQuery(ownerType, ownerId, extensionArea, extensionEntityId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        //For Tabs
        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("ui-tab-items/{ownerType}/{ownerId}/{extensionArea}")]
        public async Task<IActionResult> GetEntityExtensionUiNew(string ownerType, string ownerId, string extensionArea, CancellationToken cancellationToken)
        {
            int extensionEntityId = 0;
            var result = await _mediator.Send(new GetEntityExtensionUiNewQuery(ownerType, ownerId, extensionArea, extensionEntityId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("ui-tab-orgs/{userGuid:guid:required}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
        [ProducesResponseType(typeof(ApiResponse<List<EntityExtensionOrganisationDto>, object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEntityExtensionOrganisation(Guid userGuid, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMenuOrganisationQuery(userGuid), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        //Form schema
        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("ui-schema/{id}/{entityId}")]
        public async Task<IActionResult> GetEntityExtensionUiSchemaById(string id, string entityId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntityExtensionUiSchemaByIdQuery(id), cancellationToken);
            var permissions = new Dictionary<string, FieldPermission>();
            if (result is not null)
            {
                var itemId = await _mediator.Send(new GetItemIdBySyncGuidQuery(id), cancellationToken);
                var policyName = $"{_abacPolicyEvaluatorService.GetPolicyPrefix(result.ExtensionArea)}_fields_{result.ExId}_{itemId}";
                var resource = new Dictionary<string, object>()
                {
                    { "entityId", entityId }
                };
                permissions = (Dictionary<string, FieldPermission>)await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync(policyName, cancellationToken, resource: resource);
            }
            return Ok(new ApiResponse<object, object>(result, permissions));
        }

        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("ui-schema-arena/{id}/{entityId}")]
        public async Task<IActionResult> GetEntityExtensionUiSchemaArenaById(string id, string entityId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntityExtensionUiSchemaByIdQuery(id, true), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("schema/{ownerType}/{ownerId}/{extensionArea}/{extensionEntityId}")]
        public async Task<IActionResult> GetEntitySchema(string ownerType, int ownerId, string extensionArea, int extensionEntityId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntitySchemaQuery(ownerType, ownerId, extensionArea, extensionEntityId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("schema-arena/{ownerType}/{ownerId}/{extensionArea}/{extensionEntityId}")]
        public async Task<IActionResult> GetEntitySchemaArena(string ownerType, int ownerId, string extensionArea, int extensionEntityId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntitySchemaQuery(ownerType, ownerId, extensionArea, extensionEntityId, true), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize(Roles = "Workbench")]
        [ProducesResponseType(typeof(ApiResponse<EntityExtensionSchema, object>), StatusCodes.Status200OK)]
        [HttpGet("schema/{exId}")]
        public async Task<IActionResult> GetEntityExtensionSchemaById(int exId, [FromQuery] bool isArena = false, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new GetEntityExtensionSchemaByIdQuery(exId, isArena), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize(Roles = "Workbench")]
        [HttpPost("schema/{tabItemId:int?}")]
        public async Task<IActionResult> CreateEntitySchema([FromBody] CreateEntitySchemaCommand schema, [FromRoute] int? tabItemId, CancellationToken cancellationToken)
        {
            schema.tabItemId = tabItemId ?? -1;
            var result = await _mediator.Send(schema, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize(Roles = "Workbench")]
        [HttpDelete("form/{tabItemId?}")]
        public async Task<IActionResult> DeleteEntityExtensionForm([FromBody] DeleteEntityExtensionFormCommand schema, [FromRoute] int? tabItemId, CancellationToken cancellationToken)
        {
            schema.tabItemId = tabItemId ?? -1;
            var result = await _mediator.Send(schema, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("attachments/{mode}/{extensionArea}/{fieldId}/{docId}")]
        public async Task<IActionResult> GetEntityExtensionAttachments(string mode, string extensionArea, int fieldId, int docId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntityExtensionAttachmentsQuery(mode, extensionArea, fieldId, docId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        
        [CustomAuthorize(Roles = "Workbench")]
        [HttpPost("fieldset-attachments")]
        public async Task<IActionResult> SaveEntityExtensionFieldSetAttachment([FromBody] CreateEntityExtensionFieldsetAttachmentsCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("data/{exId}/{docId}")]
        public async Task<IActionResult> GetEntityData(int exId, int docId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEntityDataQuery(exId, docId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        //form data
        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("form-data/{exId:int}/{itemId}/{entityId:int}")]
        public async Task<IActionResult> GetSpecificEntityData(int exId, string itemId, int entityId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSpecificEntityDataQuery(exId, itemId, entityId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize(Roles = "Workbench")]
        [ProducesResponseType(typeof(ApiResponse<int, object>), StatusCodes.Status200OK)]
        [HttpPost("data")]
        public async Task<IActionResult> CreateEntityData([FromBody] CreateEntityDataCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            if (result == 0) return new EmptyResult();
            return Ok(new ApiResponse<int, object>(result));
        }

        //save form
        [CustomAuthorize(Roles = "Workbench")]
        [ProducesResponseType(typeof(ApiResponse<int, object>), StatusCodes.Status200OK)]
        [HttpPost("form-data")]
        public async Task<IActionResult> CreateSpecificEntityData([FromBody] CreateSpecificEntityDataCommand command, CancellationToken cancellationToken)
        {
            var policyName = string.Empty;
            var resource = new Dictionary<string, object>();
            var schema = await _mediator.Send(new GetEntityExtensionUiSchemaByIdQuery(command.ItemId), cancellationToken);
            var itemId = await _mediator.Send(new GetItemIdBySyncGuidQuery(command.ItemId), cancellationToken);
            if (schema.ExtensionArea.ToLowerInvariant().Equals("asset"))
            {
                policyName = "asset_edit_basicDetail";
                resource = new Dictionary<string, object>()
                {
                    { "assetRegisterId", command.EntityId.ToString() ?? string.Empty }
                };
            }
            else if (schema.ExtensionArea.ToLowerInvariant().Equals("profile"))
            {
                policyName = string.Empty;
                resource = new Dictionary<string, object>();
            }

            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "edit", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var existingRecord = await _mediator.Send(new GetSpecificEntityDataQuery(command.ExId, command.ItemId, command.EntityId), cancellationToken);
            var modifiedFields = _abacPolicyEvaluatorService.GetModifiedFields(command.Data, existingRecord);
            policyName = $"{_abacPolicyEvaluatorService.GetPolicyPrefix(schema.ExtensionArea)}_fields_{command.ExId}_{itemId}";
            resource = new Dictionary<string, object>()
                {
                    { "entityId", command.EntityId }
                };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync(policyName, cancellationToken, resource: resource);
            foreach (var field in modifiedFields)
            {
                isPermitted = true;
                if (permissions is not null && permissions.TryGetValue(field, out var fieldPermission))
                {
                    isPermitted = fieldPermission.Edit;
                }
                if (!isPermitted)
                {
                    var displayName = field;
                    if (int.TryParse(field, out var fieldId))
                    {
                        var fieldName = await _mediator.Send(new GetFieldNameByFieldIdQuery(fieldId), cancellationToken);
                        if (!string.IsNullOrWhiteSpace(fieldName))
                        {
                            displayName = fieldName;
                        }
                    }
                    _error.CustomValidation<object>($"You are not authorized to edit {displayName}");
                    return new EmptyResult();
                }
            }

            var result = await _mediator.Send(command, cancellationToken);
            if (result == 0) return new EmptyResult();
            return Ok(new ApiResponse<int, object>(result));
        }

        [CustomAuthorize(Roles = "Workbench")]
        [HttpGet("weblet-preference/{userSyncId:guid:required}/{preferenceType:required}")]
        [ProducesResponseType(typeof(ApiResponse<WebletPreference, object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWebletPreference(Guid userSyncId, string preferenceType, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetWebletPreferenceQuery(userSyncId, preferenceType), cancellationToken);
            return Ok(new ApiResponse<WebletPreference, object>(result));
        }

        [CustomAuthorize(Roles = "Workbench")]
        [HttpPost("weblet-preference")]
        public async Task<IActionResult> SaveWebletPreference([FromBody] SaveWebletPreferenceCommand command, CancellationToken cancellationToken)
        {
            int rowsAffected = await _mediator.Send(command, cancellationToken);
            if (rowsAffected == 0)
            {
                _error.CustomValidation<object>("No rows affected.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<int, object>(rowsAffected));
        }
    }
}
