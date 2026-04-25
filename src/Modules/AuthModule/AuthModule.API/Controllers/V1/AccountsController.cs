using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Asp.Versioning;
using AuthModule.Application;
using AuthModule.Application.DTOs;
using AuthModule.Application.Features.Account.Queries;
using AuthModule.Application.Features.Tenants.Commands.CreateTenant;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantClientId;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantGuid;
using AuthModule.Application.Features.Tenants.Queries.GetTenants;
using AuthModule.Application.Features.UserDeviceSessions.Commands.CreateUserDeviceSession;
using AuthModule.Application.Features.UserDeviceSessions.Queries.GetRefreshTokenExpiryDateByRefreshToken;
using AuthModule.Application.Features.Users.Commands.AuthenticateToken;
using AuthModule.Application.Features.Users.Commands.AuthenticateUser;
using AuthModule.Application.Features.Users.Commands.RefreshTokens;
using AuthModule.Application.Features.Users.Queries.GetUserByLoginId;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using AuthModule.Domain.Entities;
using Azure.Core;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace AuthModule.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
  
    [Route("api/v{version:apiVersion}/accounts")]
    [ApiController]
    [Tags("Authentication/Accounts")]
    public class AccountsController : ControllerBase
    {
        IMediator _mediator;
        private readonly IJweTokenService _jweTokenService;
        private readonly IUtilityService _utilityService;
        public AccountsController(IMediator mediator, IJweTokenService jweTokenService, IUtilityService utilityService)
        {
            _mediator = mediator;
            _jweTokenService = jweTokenService;
            _utilityService = utilityService;
        }
               
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate(AuthenticateCommand command, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(user));
        }

        [HttpPost("authenticate/by-token")]
        public async Task<IActionResult> AuthenticateByToken(AuthenticateTokenCommand command, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(user));
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpPost("token-claims")]
        public IActionResult GetTokenClaims(TokenClaim tokenClaim)
        {
            var claims = _jweTokenService.GetClaimsFromToken(tokenClaim.Token);

            if (claims == null || !claims.Any())
            {
                return BadRequest(new ApiResponse<object, object>("Invalid token or no claims found"));
            }

            string? GetClaimValue(string claimType) =>
                claims.FirstOrDefault(c => c.Type == claimType)?.Value;

            List<string> GetClaimValues(string claimType) =>
                claims.Where(c => c.Type == claimType).Select(c => c.Value).ToList();

            List<string> GetClaimValuesWithFallback(string simpleType, string fullType) =>
                claims.Where(c => c.Type == simpleType || c.Type == fullType).Select(c => c.Value).ToList();
            var response = new
            {
                UniqueGuid = GetClaimValue(JwtRegisteredClaimNames.Jti) ?? GetClaimValue("unique_name") ?? GetClaimValue("UniqueGuid"),
                UserSyncId = GetClaimValue("UserSyncId"),
                DateOfBirth = GetClaimValue("DateOfBirth"),
                TenantGuid = GetClaimValue("TenantGuid"),
                TenantClientId = GetClaimValue("TenantClientId"),
                role = GetClaimValuesWithFallback("role", ClaimTypes.Role),
                abac_role = GetClaimValues("abac_role"),
                clubs_in = GetClaimValues("clubs_in"),
                clubs_admin_of = GetClaimValues("clubs_admin_of"),
                family_members = GetClaimValues("family_members"),
                nbf = GetClaimValue(JwtRegisteredClaimNames.Nbf),
                exp = GetClaimValue(JwtRegisteredClaimNames.Exp),
                iat = GetClaimValue(JwtRegisteredClaimNames.Iat),
                iss = GetClaimValue(JwtRegisteredClaimNames.Iss),
                aud = GetClaimValue(JwtRegisteredClaimNames.Aud)
            };
            return Ok(new ApiResponse<object, object>(response));
        }

        [MapToApiVersion("1.0")]
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPasswordAsync(PasswordResetQuery resetQuery, CancellationToken cancellationToken)
        {

            var returnData = new OperationResult<bool>();
            returnData.Remark = "success";

            var result = await _mediator.Send(resetQuery, cancellationToken);
            returnData.Remark = result.Item1 ? "success" : "error";
            returnData.Data = result.Item1;
            returnData.StatusCode = 200;
            returnData.Message = result.Item2;

            return Ok(returnData);
        }


        [CustomAuthorize]
        [MapToApiVersion("1.0")]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
        {

            var returnData = new OperationResult<bool>();
            returnData.Remark = "success";

            var result = await _mediator.Send(command, cancellationToken);

            returnData.Data = result;
            returnData.StatusCode = 200;
            returnData.Message = result ? "Password changed successfully" : "Operation failed!";

            return Ok(returnData);
        }
        [AllowAnonymous]
        [HttpGet("hash-text/{plainText}")]
        public IActionResult HashText(string plainText)
        {
            // Hash password with Argon2id
            string hashedText = _utilityService.HashPassword(plainText);
            return Ok(new ApiResponse<object, object>(hashedText));
        }
        [AllowAnonymous]
        [HttpPost("verify-hash-text")]
        public IActionResult VerifyHashText(HashTextParam hashTextParam)
        {
            // Verify password with Argon2id
            bool isValid = _utilityService.VerifyPassword(hashTextParam.PlainText, hashTextParam.HashedText);
            return Ok(new ApiResponse<object, object>(isValid));
        }
        [AllowAnonymous]
        [HttpGet("encrypt-text/{plainText}")]
        public IActionResult EncryptText(string plainText)
        {
            string encryptedText = _utilityService.EncryptData2(plainText);
            return Ok(new ApiResponse<object, object>(encryptedText));
        }
        [AllowAnonymous]
        [HttpPost("decrypt-text")]
        public IActionResult DecryptText(EncryptedTextParam encryptedTextParam)
        {
            string decryptedText = _utilityService.DecryptData2(encryptedTextParam.EncryptedText);
            return Ok(new ApiResponse<object, object>(decryptedText));
        }
    }
}
