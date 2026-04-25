using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileApps.Application.Features.SystemSetting.Commands.UpdateGlobalSettings;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;

namespace MobileApps.API.Controllers.V2
{
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/generalsettings")]
    [ApiController]
    [Tags("Mobile Apps/General Settings")]
    public class GeneralSettingsController : ControllerBase
    {
        IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GeneralSettingsController(IMediator mediator, ISystemSettingsService systemSettingsService)
        {
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

    
        [HttpGet("get")]
        public async Task<IActionResult> GetGeneralAsync()
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetGlobalSettingQuery { ItemKey = "APP.GENERAL.SETTINGS" })));
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateGeneralAsync(GlobalSettingCommand command)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GlobalSettingCommand { ItemKey = "APP.GENERAL.SETTINGS", Value = command.Value, IsEncrypted = false })));
        }

        [CustomAuthorize]
        [HttpGet("help")]
        public async Task<IActionResult> GetHelpInfoAsync(CancellationToken cancellationToken)
        {
            var resultData = new Dictionary<string, object>();
            var itemKeys = "ORGANISATION.CONTACT_EMAIL_ADDRESS,ORGANISATION.CONTACT_NUMBER";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
            var emailAddress = systemSettings?.Where(w => w.ItemKey == "ORGANISATION.CONTACT_EMAIL_ADDRESS")?.Select(s => s.Value).SingleOrDefault();
            var contact = systemSettings?.Where(w => w.ItemKey == "ORGANISATION.CONTACT_NUMBER")?.Select(s => s.Value).SingleOrDefault();

            resultData.Add("Email", emailAddress.ToString());
            resultData.Add("Contact", contact.ToString());
            return Ok(new ApiResponse<object, object>(resultData));
           
        }
    }
}
