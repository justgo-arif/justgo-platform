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
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace MobileApps.API.Controllers.Global
{

    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/2FAs")]
    [ApiController]
    public class TwoFactorAuthsController : ControllerBase
    {
        IMediator _mediator;
        private readonly IMfaRepository _mfaRepository;
        private readonly ISystemSettingsService _systemSettingsService;
        public TwoFactorAuthsController(IMediator mediator, IMfaRepository mfaRepository, ISystemSettingsService systemSettingsService)
        {
            _mediator = mediator;
            _mfaRepository = mfaRepository;
            _systemSettingsService = systemSettingsService;
        }

        #region MFA API

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("send-OTP")]
        public async Task<IActionResult> SendOTPAsync(VerifyMFALoginQuery query)
        {
            var returnData = new OperationResult<bool>();
            var loginId = await _mediator.Send(new ValidateUserQuery { UserName = query.UserName, Password = query.Password });

            if (!loginId)
            {
                throw new NotFoundException("Invalid user");
            }

            var user = await _mediator.Send(query);
            if (user == null)
            {
                throw new NotFoundException("Invalid user");

            }

            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(user.Userid));

            if (userMFA == null)
            {
                throw new NotFoundException("MFA isn't enable for this user");
            }

            Dictionary<string, string> args = new Dictionary<string, string> {
                    { "whatsAppNumber", userMFA.WhatsAppNumber },
                    { "phoneCode", userMFA.PhoneCode },
                    { "email", userMFA.Email },
            };



            var userMFAResponse = new MFASetupResponseDto();

            if (query.AuthChannel.ToLower() == "emailauth")
            {
                userMFAResponse = await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = user.Userid, Email = user.EmailAddress });
            }
            else
            {
                userMFAResponse = await _mfaRepository.SetupAuthenticator(query.AuthChannel, args);
            }

            if (userMFAResponse == null)
            {
                throw new NotFoundException("MFA Setup Authenticator Failed!");
            }

            await _mediator.Send(new SaveMFALogCommand { UserId = user.Userid, Type = query.AuthChannel, Action = "LoginOTP", Args = args, Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage } });

            return Ok(new ApiResponse<object, object>(userMFAResponse.IsSuccess, 200, userMFAResponse.StatusMessage));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("validate-user")]
        public async Task<IActionResult> ValidateUserAsync(ValidateUserQuery query)
        {
            var result = await _mediator.Send(query);
            if (!result) throw new NotFoundException();

            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("enable-disable-mfa-admin")]
        public async Task<IActionResult> EnableOrDisableMfaAdminAsync(EnableDisableMFAForAdminCommand command)
        {

            if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = command.MemberDocId, InvokingUserId = command.UserId }))
            {
                throw new JustGo.Authentication.Infrastructure.Exceptions.UnauthorizedAccessException("Unauthorized to perform this operation!");
            }

            await _mediator.Send(new UpdateRememberDeviceCommand { UserId = command.UserId, IsAdmin = true });

            var result = await _mediator.Send(command);

            await _mediator.Send(new SaveMFAMandatoryUserCommand { UserId = command.MemberDocId, UpdateFlag = command.ByPassForceMFASetUpFlag });

            if (!result) throw new NotFoundException("Update failed!");


            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("enable-disable-mfa")]
        public async Task<IActionResult> EnableOrDisableMfaAsync(EnableDisableMFACommand command)
        {

            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(command.UserId));

            if (userMFA == null) throw new NotFoundException("User not Found!");

            if (userMFA != null && (userMFA.whatsAppState == 2 && command.AuthChannel.ToLower() == "whatsapp" || userMFA.AuthenticatorAppState == 2 && command.AuthChannel.ToLower() == "apthapp" || userMFA.EmailAuthState == 2 && command.AuthChannel.ToLower() == "emailauth"))
            {
                throw new NotFoundException("Failed!");
            }
            await _mediator.Send(new UpdateRememberDeviceCommand { UserId = command.UserId, IsAdmin = false });

            var result = await _mediator.Send(command);

            if (!result) throw new NotFoundException("Failed!");

            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("remove-authenticator")]
        public async Task<IActionResult> RemoveAuthenticatorAsync(RemoveAuthenticatorCommand command)
        {

            await _mediator.Send(new UpdateRememberDeviceCommand { UserId = command.UserId, IsAdmin = false });
            var result = await _mediator.Send(command);

            if (!result) throw new NotFoundException("Failed to remove authenticator!");

            return Ok(new ApiResponse<object, object>(result));

        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("get-admin-mfa-user")]
        public async Task<IActionResult> GetAdminMUserFAAsync(IsActionAllowQueryModel_V2 queryModel)
        {
            var returnData = new OperationResult<UserMFA>();
            if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = queryModel.MemberDocId, InvokingUserId = queryModel.UserId }))
            {
                throw new JustGo.Authentication.Infrastructure.Exceptions.UnauthorizedAccessException("Unauthorized to perform this operation!");
            }


            var userMFA = await _mediator.Send(new GetAdminUserMFAQuery(queryModel.MemberDocId));

            if (userMFA == null)
            {
                throw new NotFoundException("MFA isn't enable for this user");
            }

            TimeZoneMFA timeZoneMFA = await _mediator.Send(new GetTimeZoneValueQuery());

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
            returnData.Data = userMFA;

            return Ok(new ApiResponse<object, object>(returnData.Data));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpGet("get-mfa-user/{userId}")]
        public async Task<IActionResult> GetMFAUserAsync(int userId)
        {
            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(userId));

            if (userMFA == null)
            {
                throw new NotFoundException("MFA isn't enable for this user");
            }

            TimeZoneMFA timeZoneMFA = await _mediator.Send(new GetTimeZoneValueQuery());
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

        [CustomAuthorize("allowMemberProfileView", "view", "guid")]

        [MapToApiVersion("2.0")]
        [HttpGet("accounts/{guid}")]
        public async Task<IActionResult> GetMemberAccountsBySyncGuid(string guid, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(new GetUserByUserSyncIdQuery(new Guid(guid)), cancellationToken);
            var result = await _mediator.Send(new GetMfaByUserIdQuery(user.Userid), cancellationToken);
            if (result is null)
            {
                throw new NotFoundException("MFA isn't enable for this user");
            }
            TimeZoneMFA timeZoneMFA = await _mediator.Send(new GetTimeZoneValueQuery(), cancellationToken);
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

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpGet("get-backup-code/{userId}")]
        public async Task<IActionResult> GetBackupCodeAsync(int userId)
        {
            var returnData = new OperationResult<string>();


            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(userId));
            if (userMFA == null)
            {
                throw new NotFoundException("MFA isn't enable for this user");
            }
            string backupCode = _mfaRepository.GenerateBackupCode();
            bool isValidate = await _mediator.Send(new SaveBackupCodeCommand { UserId = userId, BackupCode = backupCode });

            if (!isValidate) throw new NotFoundException("Backup code generation Failed!");

            return Ok(new ApiResponse<object, object>(backupCode));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("setup-authenticator")]
        public async Task<IActionResult> SetupAuthenticatorAsync(ResendCodeQueryParam queryParam)
        {
            var returnData = new OperationResult<MFASetupResponseDto>();

            var userMFAResponse = new MFASetupResponseDto();

            if (queryParam.AuthChannel.ToLower() == "emailauth")
            {
                userMFAResponse = await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = queryParam.UserId, Email = queryParam.Args["email"] });
            }
            else
            {
                userMFAResponse = await _mfaRepository.SetupAuthenticator(queryParam.AuthChannel, queryParam.Args);
            }

            await _mediator.Send(new SaveMFALogCommand { UserId = queryParam.UserId, Type = queryParam.AuthChannel, Action = "SetupAuthenticator", Args = queryParam.Args, Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage } });

            if (userMFAResponse != null && userMFAResponse.IsSuccess && (queryParam.AuthChannel.Equals("authapp", StringComparison.OrdinalIgnoreCase) || queryParam.AuthChannel.Equals("emailAuth", StringComparison.OrdinalIgnoreCase)))
            {
                var userMfa = new UserMFA()
                {
                    AuthenticatorKey = userMFAResponse.SharedKey,
                    WhatsAppNumber = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["WhatsAppNumber"] : "",
                    PhoneCode = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["PhoneCode"] : "",
                    CountryCode = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["CountryCode"] : "",
                    Email = queryParam.Args["email"]
                };

                if (!await _mediator.Send(new SetUpMFACommand { UserId = queryParam.UserId, AuthChannel = queryParam.AuthChannel, UserMFA = userMfa }))
                {
                    throw new Exception("Setup Authenticator Failed!");

                }


            }

            return Ok(new ApiResponse<object, object>(userMFAResponse));
        }


        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCodeAsync(VerifyCodeQueryParam queryParam)
        {
            var returnData = new OperationResult<MFAVerifyDto>();
            returnData.StatusCode = 200;
            returnData.Remark = "success";

            var verifyResult = new MFAVerifyDto();

            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(queryParam.UserId));
            if (userMFA == null)
            {
                throw new NotFoundException("MFA isn't enable for this user");
            }

            if (queryParam.Args.Count == 0) queryParam.Args = new Dictionary<string, string> { { "code", queryParam.Code } };


            if (queryParam.AuthChannel.ToLower() == "emailauth")
            {
                verifyResult = await _mediator.Send(new VerifyEmailOTPQuery { UserId = queryParam.UserId, OTPCode = queryParam.Code });
            }
            else
            {
                verifyResult = await _mfaRepository.VerifyCode(queryParam.AuthChannel, queryParam.Args, userMFA);

            }
            returnData.Message = verifyResult.Message;


            await _mediator.Send(new SaveMFALogCommand { UserId = queryParam.UserId, Type = queryParam.AuthChannel, Action = queryParam.Method, Args = queryParam.Args, Obj = new { verifyResult } });

            queryParam.Code = "";
            if (verifyResult.IsValid && queryParam.AuthChannel.ToLower() == "authapp")
            {
                queryParam.Args.Clear();
                if (await _mediator.Send(new EnableDisableMFACommand { UserId = queryParam.UserId, UpdateFlag = true, AuthChannel = queryParam.AuthChannel }))
                {
                    userMFA = await _mediator.Send(new GetMfaByUserIdQuery(queryParam.UserId));
                    verifyResult.VerifyDate = queryParam.AuthChannel.ToLower() == "authapp" ? userMFA.EnableAuthenticatorAppDate?.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.AppStartDateTimezoneName : userMFA.EnableWhatsappDate?.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.WhatsAppDateTimezoneName;
                    returnData.Data = verifyResult;
                    returnData.Message = "Code verification successful";
                }
                else
                {
                    verifyResult.IsValid = false;
                    returnData.Message = "Code verification failed";
                    returnData.Remark = "error";
                    returnData.Data = verifyResult;
                }

            }
            else if (verifyResult.IsValid && queryParam.AuthChannel.ToLower() == "emailauth")
            {
                queryParam.Args.Clear();
                if (await _mediator.Send(new EnableDisableMFACommand { UserId = queryParam.UserId, UpdateFlag = true, AuthChannel = queryParam.AuthChannel }))
                {
                    userMFA = await _mediator.Send(new GetMfaByUserIdQuery(queryParam.UserId));
                    verifyResult.VerifyDate = userMFA.EmailAuthEnableDate?.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.EmailAuthEnableDateTimezoneName;
                    verifyResult.To = userMFA.Email;
                    returnData.Data = verifyResult;
                    returnData.Message = "Code verification successful";
                }
                else
                {
                    verifyResult.IsValid = false;
                    returnData.Message = "Code verification failed";
                    returnData.Remark = "error";
                    returnData.Data = verifyResult;

                }

            }
            //else if (verifyResult.IsValid && (queryParam.Method.ToLower() == "edit" || queryParam.Method.ToLower() == "setup") & queryParam.AuthChannel.ToLower() == "whatsapp")
            //{
            //    var userMfa = new UserMFA()
            //    {
            //        WhatsAppNumber = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["WhatsAppNumber"] : "",
            //        PhoneCode = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["PhoneCode"] : "",
            //        CountryCode = queryParam.AuthChannel == "whatsapp" ? queryParam.Args["CountryCode"] : ""
            //    };
            //    userMfa.WhatsAppNumber = verifyResult.To.Replace("+", "");
            //    userMfa.WhatsAppNumber = userMfa.WhatsAppNumber.Replace(userMfa.PhoneCode, "");

            //    if (await _mediator.Send(new SetUpMFACommand { UserId = queryParam.UserId, AuthChannel = queryParam.AuthChannel, UserMFA = userMfa }))
            //        if (await _mediator.Send(new EnableDisableMFACommand { UserId = queryParam.UserId, AuthChannel = queryParam.Method, UpdateFlag = true }))
            //        {
            //            userMFA = await _mediator.Send(new GetMfaByUserIdQuery(queryParam.UserId));
            //            verifyResult.VerifyDate = queryParam.AuthChannel.ToLower() == "authapp" ? userMFA.EnableAuthenticatorAppDate.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.AppStartDateTimezoneName : userMFA.EnableWhatsappDate.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.WhatsAppDateTimezoneName;

            //        }
            //    queryParam.Args.Clear();
            //    returnData.Data = verifyResult;
            //    returnData.Message = "Code verification successful";
            //}
            queryParam.Args.Clear();

            returnData.Data = verifyResult;

            return Ok(new ApiResponse<object, object>(returnData.Data, returnData.StatusCode, returnData.Message, returnData.Remark));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCodeAsync(ResendCodeQueryParam queryParam)
        {
            var returnData = new OperationResult<MFAVerifyDto>();
            returnData.StatusCode = 200;
            returnData.Remark = "success";

            var userMFAResponse = await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = queryParam.UserId, Email = queryParam.Args["email"] });
            if (!userMFAResponse.IsSuccess)
            {

                throw new NotFoundException();
            }

            await _mediator.Send(new SaveMFALogCommand { UserId = queryParam.UserId, Type = queryParam.AuthChannel, Action = "ResendCode", Args = queryParam.Args, Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage } });
            if (userMFAResponse != null && userMFAResponse.IsSuccess)
            {
                returnData.Data = new MFAVerifyDto { IsValid = true, To = userMFAResponse.To, Message = userMFAResponse.StatusMessage };
                returnData.Message = userMFAResponse.StatusMessage;

            }
            else
            {
                returnData.Data = new MFAVerifyDto { IsValid = false, To = userMFAResponse.To, Message = userMFAResponse.StatusMessage };

            }
            return Ok(new ApiResponse<object, object>(returnData.Data));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpGet("get-country-phone-code")]
        public async Task<IActionResult> GetCountryPhoneCodeAsync()
        {
            var result = await _mediator.Send(new GetCountryPhoneCodeQuery());

            if (result.Count() == 0) throw new NotFoundException();
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpGet("validate-mendatory-mfa-user/{userId}")]
        public async Task<IActionResult> ValidateMandatoryMFAUserAsync(int userId, CancellationToken cancellationToken)
        {
            var returnData = new OperationResult<bool>();

            var mfaEnabled = await _systemSettingsService.GetSystemSettings("SYSTEM.MFA.ENABLETWOFACTORAUTH", cancellationToken, "GoMembership");

            if (Convert.ToBoolean(mfaEnabled))
            {
                returnData.Data = await _mediator.Send(new ValidateMFAMandatoryUserQuery(userId));
            }
            else
            {
                throw new NotFoundException();
            }

            return Ok(new ApiResponse<object, object>(returnData.Data));

        }

        [CustomAuthorize]

        [MapToApiVersion("2.0")]
        [HttpPost("get-mendatory-mfa-user")]
        public async Task<IActionResult> GetMandatoryMFAUserAsync(IsActionAllowQueryModel_V2 queryModel)
        {
            var returnData = new OperationResult<bool>();
            returnData.Remark = "success";

            if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = queryModel.MemberDocId, InvokingUserId = queryModel.UserId }))
            {
                returnData.Message = "Unauthorized to perform this operation!";

                return Ok(new ApiResponse<object, object>(returnData.Data, 403, returnData.Message, "error"));
            }

            returnData.Data = await _mediator.Send(new GetMandatoryMFAUserDataQuery(queryModel.MemberDocId));

            returnData.Message = returnData.Data ? "Successful" : "No data Found!";
            returnData.StatusCode = returnData.Data ? 200 : 204;
            return Ok(new ApiResponse<object, object>(returnData.Data, returnData.StatusCode, returnData.Message, returnData.Remark));
        }



        #endregion  MFA API END



    }
}