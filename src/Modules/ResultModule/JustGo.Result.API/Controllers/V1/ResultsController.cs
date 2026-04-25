using Asp.Versioning;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.MemberUpload.Commands.BulkRevalidateMemberCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.CancelProcessCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.DeleteMemberCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.DeleteMemberUploadFileCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.DownloadMemberDataCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.EmailMemberUploadStatusCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.FileHeaderCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.ImportMemberCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.RevalidateMemberCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.UpdateFileStatusCommands;
using JustGo.Result.Application.Features.MemberUpload.Commands.UpdateMemberDataCommands;
using JustGo.Result.Application.Features.MemberUpload.Queries.FindAssets;
using JustGo.Result.Application.Features.MemberUpload.Queries.FindMembers;
using JustGo.Result.Application.Features.MemberUpload.Queries.GetDisciplines;
using JustGo.Result.Application.Features.MemberUpload.Queries.GetFileInformation;
using JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberData;
using JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberDetails;
using JustGo.Result.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JustGo.Result.Application.Features.MemberUpload.Queries.GetNonEditableMemberHeaders;

namespace JustGo.Result.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/results")]
    [Tags("Results/EntryValidation")]
    public class ResultsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHybridCacheService _cache;
        private readonly IUtilityService _utilityService;

        public ResultsController(IMediator mediator, IUtilityService utilityService, IHybridCacheService cache)
        {
            _mediator = mediator;
            _utilityService = utilityService;
            _cache = cache;
        }

        [CustomAuthorize]
        [HttpPost("file-headers")]
        public async Task<IActionResult> GetFileHeaders([FromForm] FileHeaderRequestDto fileDto, CancellationToken cancellationToken)
        {
            if (fileDto.File.Length == 0)
                return BadRequest(new ApiResponse<object, object>("No File Uploaded", 400, "No file uploaded."));

            const long maxFileSize = 10 * 1024 * 1024;
            if (fileDto.File.Length > maxFileSize)
                return BadRequest(new ApiResponse<object, object>("File Size Exceeded", 400,
                    "The file is more than 10 MB, Reduce the file size and try again"));
            if (string.IsNullOrEmpty(fileDto.OwnerGuid))
            {
                return BadRequest(new ApiResponse<object, object>("Invalid Owner", 400, "Owner GUID is required."));
            }
            var ownerId = await _utilityService.GetOwnerIdByGuid(fileDto.OwnerGuid, cancellationToken);
            if (ownerId == -1)
            {
                return StatusCode(403, new ApiResponse<object, object>("Forbidden", 403, "You do not have permission to access this resource."));
            }
            var flag = await _utilityService.VerifyOwnerById(ownerId, cancellationToken);
            if (!flag)
            {
                return StatusCode(403, new ApiResponse<object, object>("Forbidden", 403, "You do not have permission to access this resource."));
            }

            var command = new FileHeaderCommand(fileDto, ownerId);
            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, Ok);
        }

        [CustomAuthorize]
        [HttpPost("member-validation-upload-file")]
        public async Task<IActionResult> UploadFile([FromBody] ConfirmMemberFileDto fileDto, CancellationToken cancellationToken)
        {
            if (fileDto.FileId == 0)
                return BadRequest(new ApiResponse<object, object>("No File Id Provided", 400, "No file uploaded."));

            if (IsOperationIdAlreadyExists(fileDto.WebSocketId))
            {
                return BadRequest(new ApiResponse<object, object>("Duplicate Operation Id", 400,
                    "An operation with the same WebSocketId is already in progress. Please use a unique WebSocketId for each upload."));
            }

            var operationId = Guid.NewGuid().ToString();

            try
            {
                var command = new ImportMemberCommand(fileDto, operationId);
                var result = await _mediator.Send(command, cancellationToken);

                return FromResult(result, data => Ok(new ApiResponse<string, object>(data, 200, "File upload and processing started successfully.")));
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new ApiResponse<object, object>("Operation Cancelled", 499, "The upload operation was cancelled."));
            } 
        }

        [CustomAuthorize]
        [HttpGet("cancel-upload/{fileId:int:required}")]
        public async Task<IActionResult> CancelUpload([FromRoute] int fileId, [FromQuery] string? operationId)
        {
            try
            {
                if (operationId is not null)
                {
                    if (!LongRunningTasks.OperationIds.TryGetValue(operationId, out var cancellationTokenSource))
                    {
                        return NotFound(new ApiResponse<string, object>("Process not found.", 404,
                            "Process not found."));
                    }

                    await cancellationTokenSource.CancelAsync();
                }
                else
                {
                    var result = await _mediator.Send(new CancelImportMemberCommand(fileId));
                    return FromResult(result,
                        msg => Ok(new ApiResponse<string, object>(msg, 200, "Upload operation cancelled.")));
                }

                return Ok(new ApiResponse<string, object>("Upload operation cancelled successfully.", 200,
                    "Upload operation cancelled."));
            }
            finally
            {
                if (operationId != null && LongRunningTasks.OperationIds.TryRemove(operationId, out var tokenSource))
                {
                    tokenSource.Dispose();
                }
            }
        }

        [CustomAuthorize]
        [HttpGet("email-member-upload-status/{fileId}")]
        public async Task<IActionResult> EmailMeWhenDone([FromRoute] int fileId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new EmailMemberUploadStatusCommand(fileId), cancellationToken);
            return Ok(new ApiResponse<string, object>(result.Value, result.IsSuccess ? 200 : 400, result.Error!));
        }

        [CustomAuthorize]
        [HttpGet("disciplines")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetDiscipline([FromQuery] int scopeType, [FromQuery] string? ownerGuid, CancellationToken cancellationToken)
        {
            var sportTypeId = await _utilityService.GetTenantSportTypeAsync(cancellationToken);

            var result = await _mediator.Send(new GetDisciplinesQuery(scopeType, sportTypeId, ownerGuid), cancellationToken);
            return Ok(new ApiResponse<List<ResultDiscipline>, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("files")]
        public async Task<IActionResult> GetFiles([FromQuery] GetFileInformationQuery request,
            CancellationToken cancellationToken)
        {
            var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
            if (ownerId == -1)
            {
                return StatusCode(403, new ApiResponse<object, object>("Forbidden", 403, "You do not have permission to access this resource."));
            }
            var flag = await _utilityService.VerifyOwnerById(ownerId, cancellationToken);
            if (!flag)
            {
                return StatusCode(403, new ApiResponse<object, object>("Forbidden", 403, "You do not have permission to access this resource."));
            }

            request.OwnerId = ownerId;
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<KeysetPagedResult<FileInformationDto>, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("members")]
        public async Task<IActionResult> GetMemberData([FromQuery] GetMemberDataByFileQuery request,
            CancellationToken cancellationToken)
        {
            var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
            if (ownerId == -1)
            {
                return StatusCode(403, new ApiResponse<object, object>("Forbidden", 403, "You do not have permission to access this resource."));
            }
            var flag = await _utilityService.VerifyOwnerById(ownerId, cancellationToken);
            if (!flag)
            {
                return StatusCode(403, new ApiResponse<object, object>("Forbidden", 403, "You do not have permission to access this resource."));
            }

            request.OwnerId = ownerId;
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<KeysetPagedResult<MemberDataDto>, object>(result));
        }
        
        [CustomAuthorize]
        [HttpGet("member-details")]
        public async Task<IActionResult> GetMemberDetailsById([FromQuery] GetMemberDetailsByIdQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<MemberDetailsDto, object>(result));
        }
        
        [CustomAuthorize]
        [HttpDelete("delete-members")]
        public async Task<IActionResult> DeleteMembers([FromBody] DeleteMemberCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<string, object>(result.Value, result.IsSuccess ? 200 : 400, result.Error!));
        }
        
        [CustomAuthorize]
        [HttpPut("update-member-data")]
        public async Task<IActionResult> UpdateMemberData([FromBody] UpdateMemberDataCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<string, object>(result.Value, result.IsSuccess ? 200 : 400, result.Error!));
            //return FromResult(result, Ok);
        }
        
        [CustomAuthorize]
        [HttpPost("download-member-data")]
        public async Task<IActionResult> DownloadMemberData([FromBody] DownloadMemberDataCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<string, object>(result.Value, result.IsSuccess ? 200 : 400, result.Error!));
        }

        [CustomAuthorize]
        [HttpPut("revalidate-members")]
        public async Task<IActionResult> RevalidateMembers([FromBody] RevalidateMemberCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
        }

        [CustomAuthorize]
        [HttpPut("bulk-revalidate-members")]
        public async Task<IActionResult> BulkRevalidateMembers([FromBody] BulkRevalidateMemberCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
        }
        
        [CustomAuthorize]
        [HttpGet("find-members")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> FindMembers([FromQuery] FindMemberQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<List<FindMembersDto>, object>(result));
        }
        
        [CustomAuthorize]
        [HttpGet("find-assets")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> FindAssets([FromQuery] FindAssetQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<List<FindAssetsDto>, object>(result));
        }
        
        [CustomAuthorize]
        [HttpGet("non-editable-member-headers")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetNonEditableMemberHeaders([FromQuery] GetNonEditableMemberHeaderQuery request, CancellationToken cancellationToken)
        {
            request.SportType = await GetSportTypeFromTenant(cancellationToken);
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<List<string>, object>(result, null));
        }

        [CustomAuthorize]
        [HttpPatch("files/{fileId:int}/status/completed")]
        public async Task<IActionResult> UpdateFileStatus([FromRoute] int fileId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new UpdateFileStatusCommand(fileId, FileStatus.Completed), cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
        }
        
        [CustomAuthorize]
        [HttpPatch("files/{fileId:int}/status/archive")]
        public async Task<IActionResult> ArchiveFile([FromRoute] int fileId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new UpdateFileStatusCommand(fileId, FileStatus.Archived), cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
        }
        
        [CustomAuthorize]
        [HttpPatch("files/{fileId:int}/status/unarchive")]
        public async Task<IActionResult> UnarchiveFile([FromRoute] int fileId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new UpdateFileStatusCommand(fileId, FileStatus.Unarchived), cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
        }
        
        [CustomAuthorize]
        [HttpDelete("files/{fileId:int}")]
        public async Task<IActionResult> DeleteFile([FromRoute] int fileId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteMemberUploadFileCommand(fileId), cancellationToken);
            return FromResult(result, data => Ok(new ApiResponse<bool, object>(data)));
        }
        
        private async Task<SportType> GetSportTypeFromTenant(CancellationToken cancellationToken)
        {
            var sportTypeId = await _utilityService.GetTenantSportTypeAsync(cancellationToken);

            return (SportType)sportTypeId;
        }
        
        private static bool IsOperationIdAlreadyExists(string operationId)
        {
            return LongRunningTasks.OperationIds.ContainsKey(operationId);
        }
    }
}
