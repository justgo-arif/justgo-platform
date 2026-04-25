#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;

namespace JustGo.Authentication.Infrastructure.CustomCors
{
    public static class HttpContextExtensions
    {
        public static string? GetTenantId(this HttpContext context)
        {
            var tenantGuid = context.User.FindFirst("TenantGuid");
            if (tenantGuid != null)
                return tenantGuid.Value;

            return null;
        }
    }
}
#endif