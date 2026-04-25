using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Azure;
using System.Net.Http;


namespace MobileApps.Application.Features.Content.Query.GetUserImage
{
    class GetUserImageQueryHandler : IRequestHandler<GetUserImageQuery, FileContentResult>
    {
        private readonly IConfiguration _config;
        private readonly IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;

        public GetUserImageQueryHandler(IConfiguration config, IMediator mediator, ISystemSettingsService systemSettingsService)
        {
            _config = config;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<FileContentResult> Handle(GetUserImageQuery request, CancellationToken cancellationToken)
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
            string url = $"{baseUrl}/User/{request.UserId}/{request.ImagePath}";
            

            try
            {
                response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    return new FileContentResult(imageBytes, contentType);
                   // return data.FileContents;
                }
                else
                {
                    // Fallback to default avatar
                    return await GetFallbackImage(request.Gender, siteUrl);
                   
                }
            }
            catch
            {
                // Fallback to default avatar on exception
                return await GetFallbackImage(request.Gender, siteUrl);
               
            }
        }

        private async Task<FileContentResult> GetFallbackImage(string gender,string siteUrl)
        {
            using var _httpClient = new HttpClient();

            string url = siteUrl + "/Media/Images/";
            string img = "avatar-" + gender + ".png";
            url = url + img;

            var response = await _httpClient.GetAsync(url);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return new FileContentResult(imageBytes, contentType);
            //return data.FileContents;
        }

    }

}
