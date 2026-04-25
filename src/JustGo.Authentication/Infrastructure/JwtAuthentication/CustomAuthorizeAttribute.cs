using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using JustGo.Authentication.Infrastructure.Exceptions;
#endif

namespace JustGo.Authentication.Infrastructure.JwtAuthentication
{
#if NET9_0_OR_GREATER
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public string Roles { get; set; }
        public string PolicyName { get; set; }
        public string Action { get; set; }
        public string[] RequiredFields { get; set; }
        public CustomAuthorizeAttribute(string policyName = null, string action = null, params string[] requiredFields)
        {
            PolicyName = policyName;
            Action = action;
            RequiredFields = requiredFields ?? Array.Empty<string>();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            //Authentication
            if (!user.Identity?.IsAuthenticated ?? false)
            {
                throw new JustGo.Authentication.Infrastructure.Exceptions.UnauthorizedAccessException();
            }
            if (!string.IsNullOrWhiteSpace(Roles) && !IsInRole(user, Roles))
            {
                throw new JustGo.Authentication.Infrastructure.Exceptions.UnauthorizedAccessException();
            }

            //Authorization           
            var isAuthorized = context.HttpContext.Items["isAuthorized"];
            if (isAuthorized is null || !(bool)isAuthorized)
            {
                throw new ForbiddenAccessException();
            }

            await Task.CompletedTask;
        }

        private bool IsInRole(ClaimsPrincipal user, string roles)
        {
            var claimRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var requiredRoles = roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return requiredRoles.Any(role => claimRoles.Contains(role));
        }

    }
#endif
}
