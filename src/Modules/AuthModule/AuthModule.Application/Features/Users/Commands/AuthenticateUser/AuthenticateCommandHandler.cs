using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using YamlDotNet.Core.Tokens;

namespace AuthModule.Application.Features.Users.Commands.AuthenticateUser
{
    public class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, AuthenticateResponse>
    {
        IMediator _mediator;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LazyService<IReadRepository<User>> _readRepository;        
        private readonly IJwtAthenticationService _jwtAthenticationService;
        private readonly IUtilityService _utilityService;
        private readonly IJweTokenService _jweTokenService;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        public AuthenticateCommandHandler(IMediator mediator, IConfiguration config, IHttpContextAccessor httpContextAccessor
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

        public async Task<AuthenticateResponse> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
        {
            //string hashedPassword = _utilityService.HashPassword(request.Password);
            //bool isValid = _utilityService.VerifyPassword(request.Password, "u2vbXj/fLEVidWwgFURh8B1Z1SzMN00ggxXZsb8bG+ul3nU3gAwsjVpHS+/y/6b9");
            //var tenant = await _mediator.Send(new GetTenantByTenantGuidQuery(new Guid(request.TenantGuid)), cancellationToken);
            if (string.IsNullOrWhiteSpace(request.TenantClientId))
            {
                request.TenantClientId = await _utilityService.GetTenantClientIdByDomain(cancellationToken);
            }
            _httpContextAccessor.HttpContext.Items["tenantClientId"] = request.TenantClientId;
            var tenant = await _mediator.Send(new GetTenantByTenantClientIdQuery(request.TenantClientId), cancellationToken);
            if (tenant is null)
            {
                throw new InvalidCredentialsException();
                //throw new NotFoundException("Tenant not found");
            }
            var user = await _mediator.Send(new GetUserByLoginIdQuery(request.LoginId), cancellationToken);
            if (user is null)
            {
                throw new InvalidCredentialsException();
                //throw new NotFoundException("User not found");
            }

            var loginId = await ValidateUserorMid(request.LoginId, request.Password, cancellationToken);
            if (string.IsNullOrEmpty(loginId))
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

        private async Task<string> ValidateUserorMid(string userName, string password, CancellationToken cancellationToken)
        {
            string loginId = string.Empty;
            string pass = string.Empty;
            bool IsLocked = false;
            string p = _utilityService.Encrypt(password);
            bool IsActive = true;
            string sql = @"Select Password, IsLocked, IsActive from [User] 
                    where LoginId=@LoginId and Password=@Password";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LoginId", userName);
            queryParameters.Add("@Password", _utilityService.Encrypt(password));
            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            if(result == null)return null;
            loginId = userName;
            if (result is not null)
            {
                pass = result.Password;
                IsLocked = result.IsLocked;
                IsActive = result.IsActive;
            }

            if (string.IsNullOrWhiteSpace(pass))
            {
                sql = @"Select u.LoginId, u.[Password], u.IsLocked, u.IsActive from [User] u
                                    inner join EntityLink el on el.SourceId = u.Userid and u.[Password] = @Password
                                    inner join Members_Default md on md.DocId = el.LinkId and md.MID = @MID";
                queryParameters.Add("@MID", userName);
                queryParameters.Add("@Password", _utilityService.Encrypt(password));
                result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");

                if (result is not null)
                {
                    loginId = result.LoginId;
                    //pass = result.Password;
                    IsLocked = result.IsLocked;
                    IsActive = result.IsActive;
                }                
            }

            if (!string.IsNullOrWhiteSpace(loginId) && loginId.ToLower() != "admin")
            {
                if (!IsActive)
                    throw new Exception("User is not active");
                else if (IsLocked)
                    throw new Exception("User Locked");
            }
            return loginId;
        }



    }
}
