using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;


namespace MobileApps.Application.Features.Content.Query.GetEventImage
{
    class GetUserImageQueryHandler : IRequestHandler<GetEventImageQuery, FileContentResult>
    {

        private readonly IConfiguration _config;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetUserImageQueryHandler(IConfiguration config, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _config = config;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        

        public async Task<FileContentResult> Handle(GetEventImageQuery request, CancellationToken cancellationToken)
        {
            var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS,EVENT.DEFAULT_IMAGE";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);

            var storeRoot = systemSettings?.FirstOrDefault(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Value;
            var hostMid = systemSettings?.FirstOrDefault(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Value;
            var siteUrl = systemSettings?.FirstOrDefault(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Value;
            var eventDefaultImg = systemSettings?.Where(w => w.ItemKey == "EVENT.DEFAULT_IMAGE")?.Select(s => s.Value).SingleOrDefault();

            if (string.IsNullOrEmpty(storeRoot) || string.IsNullOrEmpty(hostMid) || string.IsNullOrEmpty(siteUrl))
            {
                // Handle missing config - return default or throw
                throw new System.Exception("Required system settings are missing.");
            }

            using var _httpClient = new HttpClient();
            HttpResponseMessage? response = null;

            string baseUrl = $"{storeRoot}/002/{hostMid}";
            string url = $"{baseUrl}/Repository/5/{request.DocId}/{request.ImagePath}";



            try
            {
                response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    return new FileContentResult(imageBytes, contentType);
                }
                else
                {
                    // Fallback to default avatar
                    return await GetFallbackImage(siteUrl, eventDefaultImg);

                }
            }
            catch
            {
                // Fallback to default avatar on exception
                return await GetFallbackImage(siteUrl, eventDefaultImg);

            }
        }

        private async Task<FileContentResult> GetFallbackImage(string siteUrl,string? eventDefaultImg)
        {
            using var _httpClient = new HttpClient();

            string url = $@"{siteUrl}/media/images/organization/EventDefaultImage/{eventDefaultImg}";


            var response = await _httpClient.GetAsync(url);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return new FileContentResult(imageBytes, contentType);

        }
    }
}
