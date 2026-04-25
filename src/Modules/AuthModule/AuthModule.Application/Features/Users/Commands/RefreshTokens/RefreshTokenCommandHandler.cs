using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid;
using AuthModule.Application.Features.UserDeviceSessions.Queries.GetRefreshTokenExpiryDateByRefreshToken;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AuthModule.Application.Features.Users.Commands.RefreshTokens
{
    public class RefreshTokenCommandHandler:IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
    {
        IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtAthenticationService _jwtAthenticationService;
        private readonly IUtilityService _utilityService;
        private readonly IJweTokenService _jweTokenService;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

        public RefreshTokenCommandHandler(IMediator mediator, IHttpContextAccessor httpContextAccessor, IJwtAthenticationService jwtAthenticationService, IUtilityService utilityService, IJweTokenService jweTokenService)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
            _jwtAthenticationService = jwtAthenticationService;
            _utilityService = utilityService;
            _jweTokenService = jweTokenService;
        }

        public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken = default)
        {
            var token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                throw new CustomValidationException("Access token is required");
            }
            var refreshToken = _httpContextAccessor.HttpContext.Request.Headers["X-Refresh-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new CustomValidationException("Refresh token is required");
            }
            var claims = _jweTokenService.GetClaimsFromToken(token); 
            var claimsLookup = claims.ToLookup(c => c.Type, c => c.Value);
            var userSyncId = claimsLookup["UserSyncId"].FirstOrDefault();
            var tenantGuid = claimsLookup["TenantGuid"].FirstOrDefault();
            var tenantClientId = claimsLookup["TenantClientId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userSyncId) || string.IsNullOrEmpty(tenantGuid) || string.IsNullOrEmpty(tenantClientId))
            {
                throw new CustomValidationException("Invalid token claims");
            }
            TenantContextManager.SetTenantClientId(tenantClientId);

            var lockKey = $"refresh_token_lock_{tenantGuid}_{userSyncId}";
            var semaphore = GetOrCreateSemaphore(lockKey);
            bool acquired = false;
            try
            {
                acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
                if (!acquired)
                {
                    throw new TimeoutException("Token refresh operation timed out. Please try again.");
                }

                var encryptedRefreshToken = _utilityService.EncryptData(refreshToken);
                var refreshTokenExpiryTask = _mediator.Send(new GetRefreshTokenExpiryDateByRefreshTokenQuery(encryptedRefreshToken), cancellationToken);
                var tenantTask = _mediator.Send(new GetTenantByTenantGuidQuery(new Guid(tenantGuid)), cancellationToken);
                var userTask = _mediator.Send(new GetUserByUserSyncIdQuery(new Guid(userSyncId)), cancellationToken);

                // Wait for all queries to complete
                await Task.WhenAll(refreshTokenExpiryTask, tenantTask, userTask);

                var refreshTokenExpiryDate = await refreshTokenExpiryTask;
                var tenant = await tenantTask;
                var user = await userTask;
                if (refreshTokenExpiryDate is null || refreshTokenExpiryDate < DateTime.UtcNow)
                {
                    throw new SecurityTokenExpiredException("Your session has expired. Please login again to continue.");
                }

                if (tenant is null || user is null)
                {
                    throw new InvalidCredentialsException("Invalid user or tenant");
                }

                //var roles = claimsLookup["role"].ToList();
                //if (roles is null || roles.Count == 0)
                //{
                //    roles = claimsLookup[ClaimTypes.Role].ToList();
                //}
                //var abacRoles = claimsLookup["abac_role"].ToList();
                //var clubsIn = claimsLookup["clubs_in"].ToList();
                //var clubsAdminOf = claimsLookup["clubs_admin_of"].ToList();
                //var familyMembers = claimsLookup["family_members"].ToList();

                //var tokenParameter = new JwtTokenParameter
                //{
                //    SecretKey = tenant.JwtAccessTokenSecretKey,
                //    ExpiryMinutes = tenant.JwtAccessTokenExpiryMinutes,
                //    Issuer = tenant.ApiUrl,
                //    Audience = tenant.TenantDomainUrl,
                //    TenantGuid = tenant.TenantGuid,
                //    TenantClientId = tenant.TenantClientId,
                //    UserName = user.LoginId,
                //    UserSyncId = new Guid(userSyncId),
                //    DateOfBirth = ((DateTime)user.DOB).Date,
                //    Roles = roles,
                //    AbacRoles = abacRoles,
                //    ClubsIn = clubsIn,
                //    ClubsAdminOf = clubsAdminOf,
                //    FamilyMembers = familyMembers
                //};

                //var newAccessToken = _jweTokenService.GenerateEncryptedAccessToken(tokenParameter);
                var newAccessToken = _jwtAthenticationService.GenerateJwtToken(tenantClientId, user.LoginId);
                var newRefreshToken = _jwtAthenticationService.GenerateRefreshToken();
                
                _utilityService.SetCookie("Authorization", $"Bearer {newAccessToken}", (double)tenant.JwtRefreshTokenExpiryMinutes);
                _utilityService.SetCookie("X-Refresh-Token", newRefreshToken, (double)tenant.JwtRefreshTokenExpiryMinutes);
                
                _jwtAthenticationService.SaveTokenIntoDB(user.Userid, (Guid)user.UserSyncId, null, newRefreshToken, tenant.JwtRefreshTokenExpiryMinutes);

                return new RefreshTokenResponse
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken
                };
            }
            finally
            {
                if (acquired)
                {
                    semaphore.Release();
                }
            }
        }
        private static SemaphoreSlim GetOrCreateSemaphore(string key)
        {
            return _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }
    }
}
