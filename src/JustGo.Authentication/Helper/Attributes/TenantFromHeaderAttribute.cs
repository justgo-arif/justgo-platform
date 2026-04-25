using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;

#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Mvc.Filters;


namespace JustGo.Authentication.Helper.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class TenantFromHeaderAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var utilityService = context.HttpContext.RequestServices.GetRequiredService<IUtilityService>();
        var tenantId = context.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = context.HttpContext.Request.Query["tenantClientId"].FirstOrDefault();
        }
        if (!string.IsNullOrEmpty(tenantId))
        {
            context.HttpContext.Items["tenantClientId"] = utilityService.DecryptData(tenantId);
        }
        base.OnActionExecuting(context);
    }
}
#endif