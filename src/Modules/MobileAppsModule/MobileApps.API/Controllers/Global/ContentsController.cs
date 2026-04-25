using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileApps.Application.Features.Content.Query.GetClassImage;
using MobileApps.Application.Features.Content.Query.GetClubImage;
using MobileApps.Application.Features.Content.Query.GetEventImage;
using MobileApps.Application.Features.Content.Query.GetUserImage;
using MobileApps.Domain.Entities.Content;
using Newtonsoft.Json;

namespace MobileApps.API.Controllers.Global 
{
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/contents")]
    [ApiController]
    [Tags("Mobile Apps/Contents")]
    public class ContentsController : ControllerBase
    {
        IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly ICryptoService _cryptoService;
        public ContentsController(IMediator mediator, ISystemSettingsService systemSettingsService, ICryptoService cryptoService)
        {
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
            _cryptoService = cryptoService;
        }

        
        [CustomAuthorize]
        [HttpGet("user-image")]
        [ProducesResponseType(typeof(ApiResponse<byte[], object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserImageEncryptAsync(string encodedPayload,CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(encodedPayload))
            {
                return Ok("valid payload is required!");
            }
            var decryptedJson = _cryptoService.DecryptObject<dynamic>(encodedPayload);

            ImageQueryParam userData = JsonConvert.DeserializeObject<ImageQueryParam>(JsonConvert.SerializeObject(decryptedJson));

            FileContentResult result= await _mediator.Send(new GetUserImageQuery { UserId = userData.UserId, ImagePath = userData.ImagePath, Gender = userData.Gender }, cancellationToken);

            Response.Headers["Cache-Control"] = "public, max-age=31536000";

            return File(result.FileContents,result.ContentType);
        }


        [CustomAuthorize]
        [HttpGet("club-image")]
        [ProducesResponseType(typeof(ApiResponse<byte[], object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClubImageAsync(string encodedPayload, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(encodedPayload))
            {
                return Ok("valid payload is required!");
            }
            var decryptedJson = _cryptoService.DecryptObject<dynamic>(encodedPayload);

            EventWithClubImages modelData = JsonConvert.DeserializeObject<EventWithClubImages>(JsonConvert.SerializeObject(decryptedJson));

            FileContentResult result = await _mediator.Send(new GetClubImageQuery { DocId=modelData.DocId,ImagePath=modelData.ImagePath,Location=modelData.Location}, cancellationToken);

            Response.Headers["Cache-Control"] = "public, max-age=31536000";

            return File(result.FileContents, result.ContentType);
           
        }

        [CustomAuthorize]
        [HttpGet("event-image")]
        [ProducesResponseType(typeof(ApiResponse<byte[], object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEventImageAsync(string encodedPayload, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(encodedPayload))
            {
                return Ok("valid payload is required!");
            }
            var decryptedJson = _cryptoService.DecryptObject<dynamic>(encodedPayload);

            EventWithClubImages modelData = JsonConvert.DeserializeObject<EventWithClubImages>(JsonConvert.SerializeObject(decryptedJson));

            FileContentResult result = await _mediator.Send(new GetEventImageQuery { DocId = modelData.DocId, ImagePath = modelData.ImagePath, Location = modelData.Location }, cancellationToken);

            Response.Headers["Cache-Control"] = "public, max-age=31536000";

            return File(result.FileContents, result.ContentType);
        }

        [CustomAuthorize]
        [MapToApiVersion("3.0")]
        [HttpGet("class-image")]
        [ProducesResponseType(typeof(ApiResponse<byte[], object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClassImageAsync(string encodedPayload, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(encodedPayload))
            {
                return Ok("valid payload is required!");
            }
            var decryptedJson = _cryptoService.DecryptObject<dynamic>(encodedPayload);

            GetClassImageQuery modelData = JsonConvert.DeserializeObject<GetClassImageQuery>(JsonConvert.SerializeObject(decryptedJson));

            FileContentResult result = await _mediator.Send(modelData, cancellationToken);

            Response.Headers["Cache-Control"] = "public, max-age=31536000";

            return File(result.FileContents, result.ContentType);
        }
    }
}
