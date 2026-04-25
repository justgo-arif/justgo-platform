using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Infrastructure.JwtAuthentication;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication
{
    public interface IJweTokenService
    {
        string GenerateEncryptedAccessToken(JwtTokenParameter tokenParameter);
        string? GetClaimFromTokenByType(string token, string claimType);
        List<Claim>? GetClaimsFromToken(string token);
        ClaimsPrincipal? GetClaimsPrincipalFromEncryptedToken(string jweToken);
        bool IsJweToken(string token);
        string CleanToken(string token);
    }
}
