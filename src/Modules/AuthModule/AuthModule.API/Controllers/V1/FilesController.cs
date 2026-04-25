using Asp.Versioning;
using AuthModule.Application.DTOs.Stores;
using AuthModule.Application.Features.Files.Commands.CreateAttachment;
using AuthModule.Application.Features.Files.Commands.DeleteAttachment;
using AuthModule.Application.Features.Files.Commands.DownloadAttachment;
using AuthModule.Application.Features.Files.Commands.FileUpload;
using AuthModule.Application.Features.Files.Commands.UploadBase64File;
using AuthModule.Application.Features.Files.Commands.UploadFile;
using AuthModule.Application.Features.Files.Commands.XUploadFile;
using AuthModule.Application.Features.Files.Queries.DownloadFile;
using AuthModule.Application.Features.Files.Queries.GetAttachments;
using AuthModule.Application.Features.Files.Queries.GetAttachmentsWithPaginations;
using AuthModule.Application.Features.Files.Queries.GetAttachmentsWithPaginationsKeyset;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
[ApiController]
[Tags("Authentication/Files")]
public class FilesController : ControllerBase
{
    readonly IMediator _mediator;
    private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
    public FilesController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
    {
        _mediator = mediator;
        _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
    }

    [CustomAuthorize]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file uploaded.");

        using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms);

        var command = new UploadFileCommand
        {
            EntityType = request.T,
            ClientUploaderRef = request.P,
            UseTemp = request.P1,
            SuccessReturnAction = request.P2,
            ErrorCallBackMethod = request.P3,
            FileName = request.File.FileName,
            FileBytes = ms.ToArray()
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("downloadTemp")]
    public async Task<IActionResult> DownloadTemp([FromQuery] string path)
    {
        var command = new DownloadTempFileQuery { Path = path };
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    [HttpGet("downloadTempR")]
    public async Task<IActionResult> DownloadTempR([FromQuery] string path)
    {
        var command = new DownloadTempFileQuery { Path = path };
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    [CustomAuthorize]
    [HttpGet("downloadAsync")]
    public async Task<IActionResult> DownloadAsync([FromQuery] string f, [FromQuery] string t, [FromQuery] string p, [FromQuery] string p1, [FromQuery] string p2, [FromQuery] string p3)
    {
        var query = new DownloadFileQuery
        {
            F = f,
            T = t,
            P = p,
            P1 = p1,
            P2 = p2,
            P3 = p3
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        var fileName = result.FileName;
        if (fileName.Contains("__$$__"))
        {
            string namePart = fileName.Split(new[] { "__$$__" }, StringSplitOptions.None)[0];
            string extension = System.IO.Path.GetExtension(fileName);
            fileName = namePart + extension;
        }

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        return File(result.FileBytes, result.ContentType, fileName);
    }

    [AllowAnonymous]
    [HttpGet("downloadPublicAsync")]
    public async Task<IActionResult> DownloadPublicAsync([FromQuery] string f, [FromQuery] string t, [FromQuery] string p, [FromQuery] string p1, [FromQuery] string p2, [FromQuery] string p3)
    {
        var query = new DownloadFileQuery
        {
            F = f,
            T = t,
            P = p,
            P1 = p1,
            P2 = p2,
            P3 = p3
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        var fileName = t == "mailattachment" ? System.IO.Path.GetFileName(result.FileName) : result.FileName;

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        return File(result.FileBytes, result.ContentType, fileName);
    }

    [CustomAuthorize]
    [HttpPost("xupload")]
    public async Task<IActionResult> XUpload([FromForm] XUploadFileCommand request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file uploaded.");

        var savedFileName = await _mediator.Send(request);
        return Content(savedFileName);
    }

    [CustomAuthorize]
    [HttpPost("uploadBase64")]
    public async Task<IActionResult> UploadBase64([FromForm] string base64String, [FromForm] string t, [FromForm] string p, [FromForm] string p1)
    {
        var command = new UploadBase64FileCommand
        {
            Base64String = base64String,
            T = t,
            P = p,
            P1 = p1
        };

        var result = await _mediator.Send(command);
        return Content(result, "text/html");
    }

    [CustomAuthorize]
    [HttpPost("file-upload")]
    public async Task<IActionResult> FileUpload([FromForm] FileUploadCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("list-attachment/{entityType}/{entityId}/{module}")]
    public async Task<IActionResult> GetAttachments(int entityType, Guid entityId, string module, CancellationToken cancellationToken)
    {
        var policyName = string.Empty;
        var resource = new Dictionary<string, object>();
        if (module.ToLowerInvariant().Equals("member"))
        {
            policyName = "member_profile_view_attachment";
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

        var result = await _mediator.Send(new GetAttachmentsQuery(entityType, entityId, module), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("list-attachment-offset")]
    public async Task<IActionResult> GetAttachments([FromQuery] GetAttachmentsWithPaginationsQuery request, CancellationToken cancellationToken)
    {
        var policyName = string.Empty;
        var resource = new Dictionary<string, object>();
        if (request.Module.ToLowerInvariant().Equals("member"))
        {
            policyName = "member_profile_view_attachment";
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
    [HttpGet("list-attachment-keyset")]
    public async Task<IActionResult> GetAttachments([FromQuery] GetAttachmentsWithPaginationsKeysetQuery request, CancellationToken cancellationToken)
    {
        var policyName = string.Empty;
        var resource = new Dictionary<string, object>();
        if (request.Module.ToLowerInvariant().Equals("member"))
        {
            policyName = "member_profile_view_attachment";
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
    [HttpPost("add-attachment")]
    public async Task<IActionResult> AddAttachment([FromForm] CreateAttachmentCommand command, CancellationToken cancellationToken)
    {
        var policyName = string.Empty;
        var resource = new Dictionary<string, object>();
        if (command.Module.ToLowerInvariant().Equals("member"))
        {
            policyName = "member_profile_add_attachment";
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
    [HttpDelete("delete-attachment/{attachmentId:guid:required}/{module:required}/{entityId:guid:required}")]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId, string module, Guid entityId, CancellationToken cancellationToken)
    {
        var policyName = string.Empty;
        var resource = new Dictionary<string, object>();
        if (module.ToLowerInvariant().Equals("member"))
        {
            policyName = "member_profile_delete_attachment";
            resource = new Dictionary<string, object>()
            {
                { "id", entityId.ToString() ?? string.Empty }
            };
        }
        var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "delete", resource, cancellationToken);
        if (!isPermitted)
        {
            throw new ForbiddenAccessException();
        }

        var result = await _mediator.Send(new DeleteAttachmentCommand(attachmentId, module, entityId), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("download-attachment/{attachmentId:guid:required}/{module:required}/{entityId:guid:required}")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId, string module, Guid entityId, CancellationToken cancellationToken)
    {
        var policyName = string.Empty;
        var resource = new Dictionary<string, object>();
        if (module.ToLowerInvariant().Equals("member"))
        {
            policyName = "member_profile_download_attachment";
            resource = new Dictionary<string, object>()
            {
                { "id", entityId.ToString() ?? string.Empty }
            };
        }
        var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "download", resource, cancellationToken);
        if (!isPermitted)
        {
            throw new ForbiddenAccessException();
        }

        var result = await _mediator.Send(new DownloadAttachmentCommand(attachmentId, module, entityId), cancellationToken);
        return File(result.FileStream, result.ContentType, result.FileName);
    }
}
