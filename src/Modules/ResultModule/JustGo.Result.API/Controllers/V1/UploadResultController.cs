using Asp.Versioning;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Result.Application.DTOs.UploadResultDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;
using JustGo.Result.Application.Features.ResultUpload.Commands.UpdateResultCompetitionStatus;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetEvents;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetResultCompetitionStatus;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetResultListByEvent;
using JustGo.Result.Application.Features.ResultUpload.Queries.PreviewResultFileQuery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadTt;
using JustGo.Result.Application.Features.ResultUpload.Commands.DeleteMember;
using JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData;
using JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands;
using JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetImportResultStatus;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDetails;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetSingleEvent;

namespace JustGo.Result.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/upload-results")]
[Tags("Results/Upload Results")]
public class UploadResultController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUtilityService _utilityService;
    private readonly IHybridCacheService _cache;
    private const long MaxFileSize = 10 * 1024 * 1014; // 10 MB

    public UploadResultController(IMediator mediator, IUtilityService utilityService, IHybridCacheService cache)
    {
        _mediator = mediator;
        _utilityService = utilityService;
        _cache = cache;
    }

    [CustomAuthorize]
    [HttpPost("confirm-header-mapping")]
    public async Task<IActionResult> UploadFile([FromBody] ConfirmMemberFileDto fileDto,
        CancellationToken cancellationToken)
    {
        if (fileDto.FileId == 0)
            return BadRequest(new ApiResponse<object, object>("No File Id Provided", 400, "No file uploaded."));

        if (IsOperationIdAlreadyExists(fileDto.WebSocketId))
        {
            return BadRequest(new ApiResponse<object, object>("Duplicate Operation ID", 400,
                "An operation with the same ID is already in progress."));
        }

        try
        {
            var sportType = await GetSportTypeFromTenant(cancellationToken);
            var command = new ImportResultFileCommand(fileDto, sportType);
            var result = await _mediator.Send(command, cancellationToken);

            return FromResult(result,
                data => Ok(new ApiResponse<string, object>(data, 200,
                    "File upload and processing started successfully.")));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499,
                new ApiResponse<object, object>("Operation Cancelled", 499, "The upload operation was cancelled."));
        }
    }

    [CustomAuthorize]
    [HttpPost("upload-result-file")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadResultFile([FromForm] UploadResultFileDto fileDto,
        CancellationToken cancellationToken)
    {
        ValidateFile(fileDto.File);

        var ownerData = await GetOwnerId(fileDto.OwnerGuid, cancellationToken);

        if (!ownerData.Status)
        {
            return StatusCode(403,
                new ApiResponse<object, object>("Forbidden", 403,
                    "You do not have permission to access this resource."));
        }

        fileDto.OwnerId = ownerData.OwnerId;

        var sportType = await GetSportTypeFromTenant(cancellationToken);
        var command = new UploadResultFileCommand(fileDto, sportType);
        var result = await _mediator.Send(command, cancellationToken);
        return FromResult(result, dto => Ok(new ApiResponse<FileHeaderResponseDto, object>(dto)));
    }


    [CustomAuthorize]
    [HttpPost("preview-result-file")]
    public async Task<IActionResult> PreviewResultFile([FromBody] GetPreviewResultFileQuery request,
        CancellationToken cancellationToken)
    {
        request.SportType = await GetSportTypeFromTenant(cancellationToken);
        var result = await _mediator.Send(request, cancellationToken);
        return FromResult(result, data => Ok(new ApiResponse<KeysetPagedResult<FilePreviewDto>, object>(data)));
    }

    [CustomAuthorize]
    [HttpGet("confirm/{uploadFileId:int:required}")]
    public async Task<IActionResult> ConfirmUploadFile(int uploadFileId, CancellationToken cancellation)
    {
        if (uploadFileId <= 0)
            return BadRequest(new ApiResponse<object, object>("InvalidUploadFileId", 400,
                "Upload file ID must be greater than 0."));

        var sportType = await GetSportTypeFromTenant(cancellation);

        var result = await _mediator.Send(new ConfirmUploadFileCommand(uploadFileId, sportType), cancellation);
        return FromResult(result, eventId => Ok(new ApiResponse<int, object>(eventId)));
    }

    [CustomAuthorize]
    [HttpPost("events")]
    public async Task<IActionResult> GetEvents([FromBody] GetEventsQuery request, CancellationToken cancellationToken)
    {
        var ownerData = await GetOwnerId(request.OwnerGuid, cancellationToken);

        if (!ownerData.Status)
        {
            return StatusCode(403,
                new ApiResponse<object, object>("Forbidden", 403,
                    "You do not have permission to access this resource."));
        }

        request.OwnerId = ownerData.OwnerId;

        var result = await _mediator.Send(request, cancellationToken);
        return FromResult(result, data => Ok(new ApiResponse<KeysetPagedResult<EventDto>, object>(data)));
    }

    [CustomAuthorize]
    [HttpPost("result-list")]
    public async Task<IActionResult> GetResultListByEventId([FromBody] GetResultListByEventIdQuery request,
        CancellationToken cancellation)
    {
        request.SportType = await GetSportTypeFromTenant(cancellation);

        var result = await _mediator.Send(request, cancellation);
        return FromResult(result, data => Ok(new ApiResponse<KeysetPagedResult<ResultListDto>, object>(data)));
    }

    [CustomAuthorize]
    [HttpGet("status")]
    public async Task<IActionResult> GetResultStatus()
    {
        var query = new GetResultCompetitionStatusQuery();
        var result = await _mediator.Send(query);
        return Ok(new ApiResponse<List<ResultCompetitionStatus>, object>(result));
    }

    [CustomAuthorize]
    [HttpPatch("update-status")]
    public async Task<IActionResult> UpdateResultStatus([FromQuery, BindRequired] int statusId,
        [FromQuery, BindRequired] int fileId)
    {
        if (statusId <= 0)
            return BadRequest(new ApiResponse<object, object>("InvalidStatusId", 400,
                "Status ID must be greater than 0."));

        var command = new UpdateResultCompetitionStatusCommand(statusId, fileId);
        var result = await _mediator.Send(command);
        return FromResult(result, _ => Ok(new ApiResponse<string, object>("Status updated successfully.")));
    }

    [CustomAuthorize]
    [HttpPut("member-data/{memberDataId:int:required}")]
    public async Task<IActionResult> UpdateMemberData([FromRoute] int memberDataId,
        [FromBody, BindRequired] Dictionary<string, string> memberData, CancellationToken cancellation)
    {
        if (memberDataId <= 0)
            return BadRequest(new ApiResponse<object, object>("InvalidUploadFileId", 400,
                "Upload file ID must be greater than 0."));

        var sportType = await GetSportTypeFromTenant(cancellation);

        var command = new UpdateMemberDataCommand(memberData, memberDataId, sportType);
        var result = await _mediator.Send(command, cancellation);

        return FromResult(result, msg => Ok(new ApiResponse<string, object>(msg)));
    }

    [CustomAuthorize]
    [HttpGet("members-data/{memberDataId:int:required}")]
    public async Task<IActionResult> GetMemberDataById([FromRoute] int memberDataId, CancellationToken cancellation)
    {
        if (memberDataId <= 0)
            return BadRequest(new ApiResponse<object, object>("InvalidMemberDataId", 400,
                "Member Data ID must be greater than 0."));

        var sportType = await GetSportTypeFromTenant(cancellation);

        var command = new GetMemberDataByIdQuery(memberDataId, sportType);
        var result = await _mediator.Send(command, cancellation);

        return FromResult(result, data => Ok(new ApiResponse<object, object>(data)));
    }

    [CustomAuthorize]
    [HttpGet("find-member/{searchTerm:required}")]
    public async Task<IActionResult> FindMember([FromRoute] string searchTerm, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return BadRequest(new ApiResponse<object, object>("InvalidSearchTerm", 400,
                "Search term cannot be empty or whitespace."));

        var command = new GetMemberDetailsQuery(searchTerm);
        var result = await _mediator.Send(command, cancellation);
        return FromResult(result, data => Ok(new ApiResponse<object, object>(data)));
    }

    [CustomAuthorize]
    [HttpPut("revalidate-member-data")]
    public async Task<IActionResult> RevalidateMemberData([FromQuery] int? fileId, [FromQuery] string? operationId,
        [FromBody] ICollection<int> memberDataIds,
        CancellationToken cancellation)
    {
        if (fileId <= 0 && memberDataIds.Count == 0)
            return BadRequest(new ApiResponse<object, object>("InvalidParameters", 400,
                "Either File ID or Member Data ID must be provided."));

        if (operationId is not null && IsOperationIdAlreadyExists(operationId))
        {
            return BadRequest(new ApiResponse<object, object>("Duplicate Operation ID", 400,
                "An operation with the same ID is already in progress."));
        }
        
        var sportType = await GetSportTypeFromTenant(cancellation);

        var command = new RevalidateMemberDataCommand(fileId, memberDataIds, sportType, operationId);
        var result = await _mediator.Send(command, cancellation);

        return FromResult(result, msg => Ok(new ApiResponse<bool, object>(msg)));
    }

    [CustomAuthorize]
    [HttpDelete("member/{fileId:int:required}")]
    public async Task<IActionResult> DeleteMemberData([FromRoute] int fileId, [FromBody] ICollection<int> memberDataIds,
        CancellationToken cancellation)
    {
        if (memberDataIds.Count == 0)
            return BadRequest(new ApiResponse<object, object>("InvalidMemberId", 400,
                "Member ID must be greater than 0."));

        var command = new DeleteMemberCommand(fileId, memberDataIds);
        var result = await _mediator.Send(command, cancellation);

        return FromResult(result, msg => Ok(new ApiResponse<bool, object>(msg)));
    }

    [CustomAuthorize]
    [HttpGet("event/{eventId:int:required}")]
    public async Task<IActionResult> GetEventById([FromRoute] int eventId, CancellationToken cancellation)
    {
        if (eventId <= 0)
            return BadRequest(new ApiResponse<object, object>("InvalidEventId", 400,
                "Event ID must be greater than 0."));

        var command = new GetSingleEventQuery(eventId);
        var result = await _mediator.Send(command, cancellation);
        return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
    }

    [CustomAuthorize]
    [HttpGet("poll-import-result-status/{fileId:int:required}")]
    public async Task<IActionResult> PollImportResultStatus([FromRoute] int fileId, CancellationToken cancellation)
    {
        if (fileId <= 0)
            return BadRequest(new ApiResponse<object, object>("InvalidFileId", 400,
                "File ID must be greater than 0."));

        var command = new GetImportResultStatusQuery(fileId);
        var result = await _mediator.Send(command, cancellation);
        return FromResult(result, data => Ok(new ApiResponse<string, object>(data)));
    }


    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file uploaded.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException("The file is more than 50 MB, Reduce the file size and try again.");
        }
    }

    private async Task<(int OwnerId, bool Status)> GetOwnerId(string ownerGuid, CancellationToken cancellation)
    {
        var ownerId = await GetCachedOwnerId(ownerGuid, cancellation);
        if (ownerId == -1)
        {
            return (-1, false);
        }

        var flag = await VerifyOwnerById(ownerId, cancellation);
        return !flag ? (-1, false) : (ownerId, true);
    }

    private async Task<int> GetCachedOwnerId(string ownerGuid, CancellationToken cancellationToken)
    {
        var cacheKey = $"OwnerId_{ownerGuid}";
        var cachedOwnerId = await _cache.TryGetAsync<int>(cacheKey, cancellationToken);
        if (cachedOwnerId.Found)
        {
            return cachedOwnerId.Value;
        }

        var ownerId = await _utilityService.GetOwnerIdByGuid(ownerGuid, cancellationToken);
        if (ownerId != -1)
        {
            await _cache.SetAsync(cacheKey, ownerId, TimeSpan.FromMinutes(30), ["JustGoClubOwnerId"],
                cancellationToken);
        }

        return ownerId;
    }

    private async Task<bool> VerifyOwnerById(int ownerId, CancellationToken cancellationToken)
    {
        var cacheKey = $"VerifyOwner_{ownerId}";
        var cachedFlag = await _cache.TryGetAsync<bool>(cacheKey, cancellationToken);
        if (cachedFlag.Found)
        {
            return cachedFlag.Value;
        }

        var flag = await _utilityService.VerifyOwnerById(ownerId, cancellationToken);
        if (flag)
        {
            await _cache.SetAsync(cacheKey, flag, TimeSpan.FromMinutes(30), ["JustGoClubOwnerId"], cancellationToken);
        }

        return flag;
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