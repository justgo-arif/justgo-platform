using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.Global;

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[ApiVersion("3.0")]
[Route("api/v{version:apiVersion}/system-settings")]
[Tags("Authentication/System Settings")]
public class SystemSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISystemSettingsService _systemSettingsService;

    public SystemSettingsController(IMediator mediator,ISystemSettingsService systemSettingsService)
    {
        _mediator = mediator;
        _systemSettingsService = systemSettingsService;
    }

    [CustomAuthorize]
    [HttpPost]
    public async Task<IActionResult> GetSystemSettingsAsync([FromBody] List<string> itemKeys,CancellationToken cancellationToken)
    {

        var requiredKeys = new[]{"SYSTEM.SITEADDRESS","SYSTEM.RESTAPI.CONFIG","ORGANISATION.TENANTCLIENTID","APPLICATION.NAME"};


        if (itemKeys == null || itemKeys.Count == 0)
            return BadRequest("Item keys are required.");



        var invalidKeys = itemKeys.Where(k => !requiredKeys.Contains(k)).ToList();

        if (invalidKeys.Any())
        {
            return BadRequest($"Invalid system setting keys (Not Permitted): {string.Join(", ", invalidKeys)}");
        }

        var systemSettings = await _systemSettingsService
            .GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);

        return Ok(new ApiResponse<object, object>(systemSettings.Select(s => new
        {
            s.ItemKey,
            s.Value
        })));
    }

   

}