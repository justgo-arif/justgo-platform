using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace JustGoAPI.Shared.Helper;

public class UserLocation
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _httpClient;

    public UserLocation(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<(string lat, string lng)> GetUserLocationAsync()
    {
        try
        {
            string location = await GetUserLocationInfoByIPAsync();
            var res = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(location);

            if (res != null && res.ContainsKey("lat") && res.ContainsKey("lon"))
            {
                return (res["lat"].ToString(), res["lon"].ToString());
            }
            else
            {
                return ("0", "0");
            }
        }
        catch (Exception)
        {
            return ("0", "0");
        }
    }

    public async Task<string> GetUserLocationInfoByIPAsync()
    {
        string ip = GetUserIP();
        if (string.IsNullOrEmpty(ip))
        {
            return string.Empty;
        }

        try
        {
            string response = await _httpClient.GetStringAsync($"http://ip-api.com/json/{ip}");
            return response;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private string GetUserIP()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return string.Empty;
        }

        // Check X-Forwarded-For header first (for proxy/load balancer scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',').First().Trim();
            return CleanIPAddress(ip);
        }

        // Fallback to remote IP address
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(remoteIp))
        {
            return CleanIPAddress(remoteIp);
        }

        return string.Empty;
    }

    private static string CleanIPAddress(string ip)
    {
        // Remove port number if present (IPv4:port format)
        if (ip.Contains(':') && ip.Count(c => c == ':') == 1)
        {
            return ip.Split(':')[0];
        }

        return ip;
    }

}