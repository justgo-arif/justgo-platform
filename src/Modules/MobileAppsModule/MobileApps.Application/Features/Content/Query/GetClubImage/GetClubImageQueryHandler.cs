using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using Microsoft.AspNetCore.Mvc;


namespace MobileApps.Application.Features.Content.Query.GetClubImage
{
    class GetClubImageQueryHandler : IRequestHandler<GetClubImageQuery, FileContentResult>
    {
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetClubImageQueryHandler(IMediator mediator, ISystemSettingsService systemSettingsService)
        {
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        
        public async Task<FileContentResult> Handle(GetClubImageQuery request, CancellationToken cancellationToken)
        {
            var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);

            var storeRoot = systemSettings?.FirstOrDefault(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Value;
            var hostMid = systemSettings?.FirstOrDefault(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Value;
            var siteUrl = systemSettings?.FirstOrDefault(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Value;

            if (string.IsNullOrEmpty(storeRoot) || string.IsNullOrEmpty(hostMid) || string.IsNullOrEmpty(siteUrl))
            {
                // Handle missing config - return default or throw
                throw new System.Exception("Required system settings are missing.");
            }

            using var _httpClient = new HttpClient();
            HttpResponseMessage? response = null;

            string baseUrl = $"{storeRoot}/002/{hostMid}";
            string url = $"{baseUrl}/Repository/2/{request.DocId}/{request.ImagePath}";

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
                    return await GetFallbackImage(siteUrl);

                }
            }
            catch
            {
                // Fallback to default avatar on exception
                return await GetFallbackImage(siteUrl);

            }
        }

        private async Task<FileContentResult> GetFallbackImage(string siteUrl)
        {
            using var _httpClient = new HttpClient();

           string url =$@"{siteUrl}/Media/Images/club-default.png";

            var response = await _httpClient.GetAsync(url);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return new FileContentResult(imageBytes, contentType);
           
        }
    }
}
