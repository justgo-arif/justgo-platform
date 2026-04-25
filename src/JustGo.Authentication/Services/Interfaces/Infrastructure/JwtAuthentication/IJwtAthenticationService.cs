using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication
{
    public interface IJwtAthenticationService
    {
        string GenerateJwtToken(string tenantClientId, string loginId);
        string GenerateAccessToken(JwtTokenParameter tokenParameter);
#if NET9_0_OR_GREATER
        void AttachUserToContext(HttpContext context, string jwtToken, dynamic tenant);
#endif        
        string GenerateRefreshToken(int size = 32);
        ClaimsPrincipal? GetClaimsPrincipal(string jwtToken);        
        int SaveTokenIntoDB(int userId, Guid userSyncId, string accessToken, string refreshToken, int refreshTokenExpiryMinutes);
        dynamic? GetTenantByTenantClientId(string tenantClientId);
    }
}
