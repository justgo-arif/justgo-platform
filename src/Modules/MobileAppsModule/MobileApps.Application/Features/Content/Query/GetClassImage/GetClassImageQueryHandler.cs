using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MobileApps.Application.Features.Content.Query.GetClassImage
{
    class GetClassImageQueryHandler : IRequestHandler<GetClassImageQuery, FileContentResult>
    {

        private readonly IConfiguration _config;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetClassImageQueryHandler(IConfiguration config, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _config = config;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        

        public async Task<FileContentResult> Handle(GetClassImageQuery request, CancellationToken cancellationToken)
        {
            var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS,EVENT.DEFAULT_IMAGE";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
            var storeRoot = systemSettings?.Where(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Select(s => s.Value).SingleOrDefault();
            var hostMid = systemSettings?.Where(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Select(s => s.Value).SingleOrDefault();
            var siteUrl = systemSettings?.Where(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Select(s => s.Value).SingleOrDefault();
            var eventDefaultImg = systemSettings?.Where(w => w.ItemKey == "EVENT.DEFAULT_IMAGE")?.Select(s => s.Value).SingleOrDefault();

            if (string.IsNullOrEmpty(storeRoot) || string.IsNullOrEmpty(hostMid) || string.IsNullOrEmpty(siteUrl))
            {
                // Handle missing config - return default or throw
                throw new System.Exception("Required system settings are missing.");
            }

            using var _httpClient = new HttpClient();
            HttpResponseMessage? response = null;

            string baseUrl = $"{storeRoot}/002/{hostMid}";
            string url = baseUrl + "/justgobookingattachment/" + request.EntityTypeId + "/" + request.ClassId+ "/" + request.Location;

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
                    return await GetFallbackImage(siteUrl, request.StorePath, request.Location);

                }
            }
            catch
            {
                // Fallback to default avatar on exception
                return await GetFallbackImage(siteUrl, request.StorePath, request.Location);

            }
        }

        private async Task<FileContentResult> GetFallbackImage(string siteUrl,string storePath,string location)
        {
            using var _httpClient = new HttpClient();

            string url = $"{siteUrl}{storePath}{location}";

            var response = await _httpClient.GetAsync(url);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return new FileContentResult(imageBytes, contentType);
           
        }
    }
}
