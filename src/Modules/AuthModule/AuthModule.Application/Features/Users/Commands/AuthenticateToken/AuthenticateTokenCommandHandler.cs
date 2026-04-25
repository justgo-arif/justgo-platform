using AuthModule.Application.DTOs;
using AuthModule.Application.Features.ClubMembers.Queries.GetAdminClubsWithChildByUserId;
using AuthModule.Application.Features.ClubMembers.Queries.GetClubsAdminByUserId;
using AuthModule.Application.Features.ClubMembers.Queries.GetClubsByUserId;
using AuthModule.Application.Features.ClubMembers.Queries.GetFamilyMembersByUserId;
using AuthModule.Application.Features.Groups.Queries.GetGroupsByUserId;
using AuthModule.Application.Features.Memberships.Queries.GetMembershipByUserId;
using AuthModule.Application.Features.Roles.Queries.GetRolesByUser;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByDomain;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantClientId;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid;
using AuthModule.Application.Features.UserDeviceSessions.Commands.CreateUserDeviceSession;
using AuthModule.Application.Features.Users.Queries.GetUserByLoginId;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace AuthModule.Application.Features.Users.Commands.AuthenticateToken
{
    public class AuthenticateTokenCommandHandler : IRequestHandler<AuthenticateTokenCommand, AuthenticateResponse>
    {
        IMediator _mediator;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LazyService<IReadRepository<User>> _readRepository;
        private readonly IJwtAthenticationService _jwtAthenticationService;
        private readonly IUtilityService _utilityService;
        private readonly IJweTokenService _jweTokenService;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        public AuthenticateTokenCommandHandler(IMediator mediator, IConfiguration config, IHttpContextAccessor httpContextAccessor
            , LazyService<IReadRepository<User>> readRepository, IJwtAthenticationService jwtAthenticationService
            , IUtilityService utilityService, IJweTokenService jweTokenService
            , IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _readRepository = readRepository;
            _jwtAthenticationService = jwtAthenticationService;
            _utilityService = utilityService;
            _jweTokenService = jweTokenService;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        public async Task<AuthenticateResponse> Handle(AuthenticateTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TenantClientId))
            {
                request.TenantClientId = await _utilityService.GetTenantClientIdByDomain(cancellationToken);
            }

            string tenantClientId = _utilityService.DecryptData(request.TenantClientId);
            _httpContextAccessor.HttpContext.Items["tenantClientId"] = tenantClientId;  
            var tenant = await _mediator.Send(new GetTenantByTenantClientIdQuery(tenantClientId), cancellationToken);
            if (tenant is null)
            {
                throw new InvalidCredentialsException();
                //throw new NotFoundException("Tenant not found");
            }
            var userSyncId = ValidateJwtToken(request.Token);
            string userName = await ValidateToken(userSyncId, request.Token,cancellationToken);

            var user = await _mediator.Send(new GetUserByLoginIdQuery(userName), cancellationToken);
            if (user is null)
            {
                throw new InvalidCredentialsException();
                //throw new NotFoundException("User not found");
            }

            //var loginId = await ValidateUserorMid(request.LoginId, request.Password, cancellationToken);
            //var loginId = await ValidateUserorMid(request.Token, request.Token, cancellationToken);

            if (string.IsNullOrEmpty(userName))
            {
                throw new InvalidCredentialsException();
            }

            var accessToken = _jwtAthenticationService.GenerateJwtToken(tenant.TenantClientId, user.LoginId);

            var appType = _httpContextAccessor.HttpContext.Request.Headers["X-App-Type"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(appType) && appType.Equals("Mobile"))
            {
                var claims = _jweTokenService.GetClaimsFromToken(accessToken);
                var claimsLookup = claims.ToLookup(c => c.Type, c => c.Value);
                var abacRoles = claimsLookup["abac_role"].ToList();
                var userAttributes = new Dictionary<string, object>()
                {
                    { "abacRoles", abacRoles }
                };
                var isAuthorized = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("mobile_allow_system_access", null, userAttributes, null, cancellationToken);
                if (!isAuthorized)
                {
                    throw new ForbiddenAccessException("User is not authorized to access the system.");
                }
            }
            var refreshToken = _jwtAthenticationService.GenerateRefreshToken();
            _utilityService.SetCookie("Authorization", $"Bearer {accessToken}", (double)tenant.JwtRefreshTokenExpiryMinutes);
            _utilityService.SetCookie("X-Refresh-Token", refreshToken, (double)tenant.JwtRefreshTokenExpiryMinutes);
            _jwtAthenticationService.SaveTokenIntoDB(user.Userid, (Guid)user.UserSyncId, null, refreshToken, tenant.JwtRefreshTokenExpiryMinutes);

            return new AuthenticateResponse
            {
                Userid = user.Userid,
                LoginId = user.LoginId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailAddress = user.EmailAddress,
                DOB = user.DOB,
                Gender = user.Gender,
                MemberDocId = user.MemberDocId,
                MemberId = user.MemberId,
                UserSyncId = user.UserSyncId,
                ProfileImageUrl = user.ProfilePicURL,
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }


        public string ValidateJwtToken(string token)
        {
            TokenValidationParameters validationParameters = new TokenValidationParameters();
            validationParameters.ValidateLifetime = false;
            validationParameters.ValidateAudience = false;
            validationParameters.ValidateIssuer = false;
            validationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("c66p&&3oq)tcp&4dery4&+q@o9_sob@0e0u5-%iokfsj%7$8h*Qh=rQqBz6=sjEroI#gO5o56)&A9KPYcj&uLprf@w&xZ)Jh*Hrpm=t!u+(2hekch389o7bye3*n&@*")); // The same key as the one that generate the token

            var tokenHandler = new JwtSecurityTokenHandler();

            SecurityToken validatedToken;

            if (tokenHandler.CanReadToken(token))
            {
                    ClaimsPrincipal principal = tokenHandler.ValidateToken(token.ToString(), validationParameters, out validatedToken);

                    var tokenExp = principal.Claims.First(claim => claim.Type.Equals("exp")).Value;

                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt32(tokenExp));
                    DateTime dateTime = dateTimeOffset.UtcDateTime;

                    if (principal.HasClaim(c => c.Type == ClaimTypes.Sid) && dateTime > DateTime.UtcNow)
                    {

                        var memberGuid = principal.Claims.Where(c => c.Type == ClaimTypes.Sid).First().Value;
                       return memberGuid;
                    }
            }
            return string.Empty;
        }

        private async Task<string> ValidateToken(string userSyncId,string token, CancellationToken cancellationToken)
        {


            string userName = string.Empty;

            string sql = @"select top 1 UserName LoginId from LoginSessionToken where SyncGuid = @SyncGuid and Token = @Token and IsValid = 1 and ExpiredAd > = getdate()";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Token", token);
            queryParameters.Add("@SyncGuid", userSyncId);
            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            if (result == null) return null;

            if (result is not null)
            {
                userName = result.LoginId;
            }

            return userName;
        }
    }
}