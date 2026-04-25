using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using AuthModule.Application.DTOs.MFA;
using AuthModule.Application.Features.MFA.Commands.Create;
using AuthModule.Application.Features.MFA.Commands.Delete;
using AuthModule.Application.Features.MFA.Commands.Update;
using AuthModule.Application.Features.MFA.Queries.GetCountryPhoneCode;
using AuthModule.Application.Features.MFA.Queries.GetMandatoryMFAUserDataByUserId;
using AuthModule.Application.Features.MFA.Queries.GetMfaByUserId;
using AuthModule.Application.Features.MFA.Queries.GetTimeZoneValue;
using AuthModule.Application.Features.MFA.Queries.ResendCodeQueryParam;
using AuthModule.Application.Features.MFA.Queries.ValidateMandatoryMFAByUserId;
using AuthModule.Application.Features.MFA.Queries.ValidateUser;
using AuthModule.Application.Features.MFA.Queries.VerifyCodeQueryParam;
using AuthModule.Application.Features.MFA.Queries.VerifyMFALogin;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using AuthModule.Application.Interfaces.Persistence.Repositories.MFARepository;
using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MobileApps.API.Controllers.Global
{
    [ApiController]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/mfas")]
    [Tags("Mobile Apps/Multi-Factor Authentications (MFAs)")]
    public class MultiFactorAuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMfaRepository _mfaRepository;
        private readonly ISystemSettingsService _systemSettingsService;

        public MultiFactorAuthController(
            IMediator mediator,
            IMfaRepository mfaRepository,
            ISystemSettingsService systemSettingsService)
        {
            _mediator = mediator;
            _mfaRepository = mfaRepository;
            _systemSettingsService = systemSettingsService;
        }

        /// <summary>
        /// Send OTP for MFA login.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtpAsync([FromBody] VerifyMFALoginQuery query)
        {
            var isValidUser = await _mediator.Send(new ValidateUserQuery { UserName = query.UserName, Password = query.Password });
            if (!isValidUser)
                return Ok(new ApiResponse<object, object>(null, 200, "Invalid user"));

            var user = await _mediator.Send(query);
            if (user == null)
                return Ok(new ApiResponse<object, object>(null, 200, "Invalid user"));

            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(user.Userid));
            if (userMFA == null)
                return Ok(new ApiResponse<object, object>(null, 200, "MFA isn't enabled for this user"));

            var args = new Dictionary<string, string>
          {
              { "whatsAppNumber", userMFA.WhatsAppNumber },
              { "phoneCode", userMFA.PhoneCode },
              { "email", userMFA.Email }
          };

            MFASetupResponseDto userMFAResponse =
                query.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase)
                    ? await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = user.Userid, Email = user.EmailAddress })
                    : await _mfaRepository.SetupAuthenticator(query.AuthChannel, args);

            if (userMFAResponse == null)
                return Ok(new ApiResponse<object, object>(null, 500, "MFA Setup Authenticator Failed!", "error"));

            await _mediator.Send(new SaveMFALogCommand
            {
                UserId = user.Userid,
                Type = query.AuthChannel,
                Action = "LoginOTP",
                Args = args,
                Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage }
            });

            return Ok(new ApiResponse<object, object>(
                userMFAResponse,
                200,
                userMFAResponse.StatusMessage

            ));
        }

        /// <summary>
        /// Validate user credentials.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("validate-user")]
        public async Task<IActionResult> ValidateUserAsync([FromBody] ValidateUserQuery query)
        {
            var result = await _mediator.Send(query);
            if (!result)
                return Ok(new ApiResponse<object, object>(false, 200, "Invalid user"));

            return Ok(new ApiResponse<object, object>(result));
        }

        /// <summary>
        /// Enable or disable MFA for admin.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("enable-disable-mfa-admin")]
        public async Task<IActionResult> EnableOrDisableMfaAdminAsync([FromBody] EnableDisableMFAForAdminCommand command)
        {
            if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = command.MemberDocId, InvokingUserId = command.UserId }))
                return Ok(new ApiResponse<object, object>(false, 200, "Unauthorized to perform this operation!"));

            await _mediator.Send(new UpdateRememberDeviceCommand { UserId = command.UserId, IsAdmin = true });
            var result = await _mediator.Send(command);
            await _mediator.Send(new SaveMFAMandatoryUserCommand { UserId = command.MemberDocId, UpdateFlag = command.ByPassForceMFASetUpFlag });

            if (!result)
                return Ok(new ApiResponse<object, object>(false, 200, "Update failed!"));

            return Ok(new ApiResponse<object, object>(result));
        }

        /// <summary>
        /// Enable or disable MFA for user.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("enable-disable-mfa")]
        public async Task<IActionResult> EnableOrDisableMfaAsync([FromBody] EnableDisableMFACommand command)
        {
            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(command.UserId));
            if (userMFA == null)
                return Ok(new ApiResponse<object, object>(null, 200, "User not found!"));

            if ((userMFA.whatsAppState == 2 && command.AuthChannel.Equals("whatsapp", StringComparison.OrdinalIgnoreCase)) ||
                (userMFA.AuthenticatorAppState == 2 && command.AuthChannel.Equals("apthapp", StringComparison.OrdinalIgnoreCase)) ||
                (userMFA.EmailAuthState == 2 && command.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase)))
            {
                return Ok(new ApiResponse<object, object>(null, 200, "Failed!"));
            }

            await _mediator.Send(new UpdateRememberDeviceCommand { UserId = command.UserId, IsAdmin = false });
            var result = await _mediator.Send(command);

            if (!result)
                return Ok(new ApiResponse<object, object>(false, 200, "Failed!"));

            return Ok(new ApiResponse<object, object>(result));
        }

        /// <summary>
        /// Remove authenticator for user.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("remove-authenticator")]
        public async Task<IActionResult> RemoveAuthenticatorAsync([FromBody] RemoveAuthenticatorCommand command)
        {
            await _mediator.Send(new UpdateRememberDeviceCommand { UserId = command.UserId, IsAdmin = false });
            var result = await _mediator.Send(command);

            if (!result)
                return Ok(new ApiResponse<object, object>(false, 200, "Failed to remove authenticator!"));

            return Ok(new ApiResponse<object, object>(result));
        }

        /// <summary>
        /// Get admin MFA user.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("admin-mfa-user")]
        public async Task<IActionResult> GetAdminMUserFAAsync([FromBody] IsActionAllowQueryModel_V2 queryModel)
        {
            if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = queryModel.MemberDocId, InvokingUserId = queryModel.UserId }))
                return Ok(new ApiResponse<object, object>(false, 200, "Unauthorized to perform this operation!"));

            var userMFA = await _mediator.Send(new GetAdminUserMFAQuery(queryModel.MemberDocId));
            if (userMFA == null)
                return Ok(new ApiResponse<object, object>(null, 200, "MFA isn't enabled for this user"));

            var timeZoneMFA = await _mediator.Send(new GetTimeZoneValueQuery());
            if (userMFA.EnableAuthenticatorAppDate == DateTime.MinValue)
            {
                userMFA.EnableAuthenticatorAppDate = timeZoneMFA.Date;
                userMFA.AppStartDateTimezoneName = timeZoneMFA.Name;
            }
            if (userMFA.EnableWhatsappDate == DateTime.MinValue)
            {
                userMFA.EnableWhatsappDate = timeZoneMFA.Date;
                userMFA.WhatsAppDateTimezoneName = timeZoneMFA.Name;
            }
            if (userMFA.EmailAuthEnableDate == DateTime.MinValue)
            {
                userMFA.EmailAuthEnableDate = timeZoneMFA.Date;
                userMFA.EmailAuthEnableDateTimezoneName = timeZoneMFA.Name;
            }

            return Ok(new ApiResponse<object, object>(userMFA));
        }

        /// <summary>
        /// Get MFA user by userId.
        /// </summary>
        [CustomAuthorize]
        [HttpGet("mfa-user/{userId:int}")]
        public async Task<IActionResult> GetMFAUserAsync([FromRoute] int userId)
        {
            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(userId));
            if (userMFA == null)
                return Ok(new ApiResponse<object, object>(new UserMFA(), 200, "MFA isn't enabled for this user"));

            var timeZoneMFA = await _mediator.Send(new GetTimeZoneValueQuery());
            if (userMFA.EnableAuthenticatorAppDate == DateTime.MinValue)
            {
                userMFA.EnableAuthenticatorAppDate = timeZoneMFA.Date;
                userMFA.AppStartDateTimezoneName = timeZoneMFA.Name;
            }
            if (userMFA.EnableWhatsappDate == DateTime.MinValue)
            {
                userMFA.EnableWhatsappDate = timeZoneMFA.Date;
                userMFA.WhatsAppDateTimezoneName = timeZoneMFA.Name;
            }

            return Ok(new ApiResponse<object, object>(userMFA));
        }

        /// <summary>
        /// Get member accounts by sync guid. (Unchanged as requested)
        /// </summary>
        [CustomAuthorize("allowMemberProfileView", "view", "guid")]
        [HttpGet("accounts/{guid:guid}")]
        public async Task<IActionResult> GetMemberAccountsBySyncGuid([FromRoute] string guid, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(new GetUserByUserSyncIdQuery(new Guid(guid)), cancellationToken);
            var result = await _mediator.Send(new GetMfaByUserIdQuery(user.Userid), cancellationToken);
            if (result is null)
                throw new NotFoundException("MFA isn't enabled for this user");

            var timeZoneMFA = await _mediator.Send(new GetTimeZoneValueQuery(), cancellationToken);
            if (result.EnableAuthenticatorAppDate == DateTime.MinValue)
            {
                result.EnableAuthenticatorAppDate = timeZoneMFA.Date;
                result.AppStartDateTimezoneName = timeZoneMFA.Name;
            }
            if (result.EnableWhatsappDate == DateTime.MinValue)
            {
                result.EnableWhatsappDate = timeZoneMFA.Date;
                result.WhatsAppDateTimezoneName = timeZoneMFA.Name;
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        /// <summary>
        /// Get backup code for user.
        /// </summary>
        [CustomAuthorize]
        [HttpGet("backup-code/{userId:int}")]
        public async Task<IActionResult> GetBackupCodeAsync([FromRoute] int userId)
        {
            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(userId));
            if (userMFA == null)
                return Ok(new ApiResponse<object, object>(null, 200, "MFA isn't enabled for this user"));

            string backupCode = _mfaRepository.GenerateBackupCode();
            bool isValidate = await _mediator.Send(new SaveBackupCodeCommand { UserId = userId, BackupCode = backupCode });

            if (!isValidate)
                return Ok(new ApiResponse<object, object>(null, 500, "Backup code generation failed!", "error"));

            return Ok(new ApiResponse<object, object>(backupCode));
        }

        /// <summary>
        /// Setup authenticator for user.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("setup-authenticator")]
        public async Task<IActionResult> SetupAuthenticatorAsync([FromBody] ResendCodeQueryParam queryParam)
        {
            MFASetupResponseDto userMFAResponse =
                queryParam.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase)
                    ? await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = queryParam.UserId, Email = queryParam.Args["email"] })
                    : await _mfaRepository.SetupAuthenticator(queryParam.AuthChannel, queryParam.Args);

            await _mediator.Send(new SaveMFALogCommand
            {
                UserId = queryParam.UserId,
                Type = queryParam.AuthChannel,
                Action = "SetupAuthenticator",
                Args = queryParam.Args,
                Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage }
            });

            if (userMFAResponse != null && userMFAResponse.IsSuccess &&(queryParam.AuthChannel.Equals("authapp", StringComparison.OrdinalIgnoreCase) || queryParam.AuthChannel.Equals("emailAuth", StringComparison.OrdinalIgnoreCase)))
            {
                var userMfa = new UserMFA
                {
                    AuthenticatorKey = userMFAResponse.SharedKey,
                    WhatsAppNumber = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["WhatsAppNumber"] : "",
                    PhoneCode = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["PhoneCode"] : "",
                    CountryCode = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["CountryCode"] : "",
                    Email = queryParam.Args["email"]
                };

                bool setupResult = await _mediator.Send(new SetUpMFACommand { UserId = queryParam.UserId, AuthChannel = queryParam.AuthChannel, UserMFA = userMfa });
                if (!setupResult)
                    return Ok(new ApiResponse<object, object>(null, 500, "Setup Authenticator Failed!", "error"));
            }

            return Ok(new ApiResponse<object, object>(userMFAResponse));
        }

        /// <summary>
        /// Verify MFA code.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCodeAsync([FromBody] VerifyCodeQueryParam queryParam)
        {
            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(queryParam.UserId));
            if (userMFA == null)
                return Ok(new ApiResponse<object, object>(null, 200, "MFA isn't enabled for this user"));

            if (queryParam.Args.Count == 0)
                queryParam.Args = new Dictionary<string, string> { { "code", queryParam.Code } };

            MFAVerifyDto verifyResult =
                queryParam.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase)
                    ? await _mediator.Send(new VerifyEmailOTPQuery { UserId = queryParam.UserId, OTPCode = queryParam.Code })
                    : await _mfaRepository.VerifyCode(queryParam.AuthChannel, queryParam.Args, userMFA);

            await _mediator.Send(new SaveMFALogCommand
            {
                UserId = queryParam.UserId,
                Type = queryParam.AuthChannel,
                Action = queryParam.Method,
                Args = queryParam.Args,
                Obj = new { verifyResult }
            });

            queryParam.Code = "";
            queryParam.Args.Clear();

            if (verifyResult.IsValid)
            {
                bool enabled = await _mediator.Send(new EnableDisableMFACommand { UserId = queryParam.UserId, UpdateFlag = true, AuthChannel = queryParam.AuthChannel });
                if (enabled)
                {
                    userMFA = await _mediator.Send(new GetMfaByUserIdQuery(queryParam.UserId));
                    verifyResult.VerifyDate = queryParam.AuthChannel.Equals("authapp", StringComparison.OrdinalIgnoreCase)
                        ? userMFA.EnableAuthenticatorAppDate?.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.AppStartDateTimezoneName
                        : userMFA.EmailAuthEnableDate?.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.EmailAuthEnableDateTimezoneName;

                    if (queryParam.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase))
                        verifyResult.To = userMFA.Email;

                    return Ok(new ApiResponse<object, object>(verifyResult, 200, "Code verification successful"));
                }
            }

            verifyResult.IsValid = false;
            return Ok(new ApiResponse<object, object>(verifyResult, 200, "Code verification failed"));
        }

        /// <summary>
        /// Resend MFA code.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCodeAsync([FromBody] ResendCodeQueryParam queryParam)
        {
            var userMFAResponse = await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = queryParam.UserId, Email = queryParam.Args["email"] });
            if (!userMFAResponse.IsSuccess)
                return Ok(new ApiResponse<object, object>(null, 200, "Failed to resend code"));

            await _mediator.Send(new SaveMFALogCommand
            {
                UserId = queryParam.UserId,
                Type = queryParam.AuthChannel,
                Action = "ResendCode",
                Args = queryParam.Args,
                Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage }
            });

            var returnData = new MFAVerifyDto { IsValid = true, To = userMFAResponse.To, Message = userMFAResponse.StatusMessage };
            return Ok(new ApiResponse<object, object>(returnData, 200, userMFAResponse.StatusMessage));
        }

        /// <summary>
        /// Get country phone codes.
        /// </summary>
        [CustomAuthorize]
        [HttpGet("country-phone-code")]
        public async Task<IActionResult> GetCountryPhoneCodeAsync()
        {
            var result = await _mediator.Send(new GetCountryPhoneCodeQuery());
            if (!result.Any())
                return Ok(new ApiResponse<object, object>(null, 200, "No data found"));

            return Ok(new ApiResponse<object, object>(result));
        }

        /// <summary>
        /// Validate if mandatory MFA is enabled for user.
        /// </summary>
        [CustomAuthorize]
        [HttpGet("validate-mandatory-mfa-user/{userId:int}")]
        public async Task<IActionResult> ValidateMandatoryMFAUserAsync([FromRoute] int userId, CancellationToken cancellationToken)
        {
            var mfaEnabled = await _systemSettingsService.GetSystemSettings("SYSTEM.MFA.ENABLETWOFACTORAUTH", cancellationToken, "GoMembership");
            if (!Convert.ToBoolean(mfaEnabled))
                return Ok(new ApiResponse<object, object>(false, 200, "Mandatory MFA not enabled"));

            var isMandatory = await _mediator.Send(new ValidateMFAMandatoryUserQuery(userId));
            return Ok(new ApiResponse<object, object>(isMandatory));
        }

        /// <summary>
        /// Get mandatory MFA user.
        /// </summary>
        [CustomAuthorize]
        [HttpPost("mandatory-mfa-user")]
        public async Task<IActionResult> GetMandatoryMFAUserAsync([FromBody] IsActionAllowQueryModel_V2 queryModel)
        {
            if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = queryModel.MemberDocId, InvokingUserId = queryModel.UserId }))
                return Ok(new ApiResponse<object, object>(false, 403, "Unauthorized to perform this operation!", "error"));

            var isMandatory = await _mediator.Send(new GetMandatoryMFAUserDataQuery(queryModel.MemberDocId));
            var statusCode = 200;
            var message = isMandatory ? "Successful" : "No data found";

            return Ok(new ApiResponse<object, object>(isMandatory, statusCode, message));
        }

    }

}
