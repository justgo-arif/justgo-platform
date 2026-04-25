using Asp.Versioning;
using AuthModule.Application.DTOs.MFA;
using AuthModule.Application.Features.MFA.Commands.Create;
using AuthModule.Application.Features.MFA.Commands.Delete;
using AuthModule.Application.Features.MFA.Commands.Update;
using AuthModule.Application.Features.MFA.Queries.GetCountryPhoneCode;
using AuthModule.Application.Features.MFA.Queries.GetMandatoryMFAUserDataByUserId;
using AuthModule.Application.Features.MFA.Queries.GetMfaByUserGuid;
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
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberBasicInfoBySyncGuid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.Global;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mfa")]
[Tags("Authentication/Two Factor Authentication (MFA)")]
public class MultiFactorAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMfaRepository _mfaRepository;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly ICustomError _error;
    private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

    public MultiFactorAuthController(
        IMediator mediator,
        IMfaRepository mfaRepository,
        ISystemSettingsService systemSettingsService,
        ICustomError error,
        IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
    {
        _mediator = mediator;
        _mfaRepository = mfaRepository;
        _systemSettingsService = systemSettingsService;
        _error = error;
        _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
    }

    [CustomAuthorize]
    [HttpGet("mfa-user/{userGuid:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<UserMFA, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMFAUserAsync([FromRoute] Guid userGuid, CancellationToken cancellationToken)
    {
        var resource = new Dictionary<string, object>()
            {
                { "userSyncId", userGuid }
            };
        var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("member_2fa_update_ui", cancellationToken, null, resource);

        var userMFA = await _mediator.Send(new GetMfaByUserGuidQuery(userGuid));
        if (userMFA == null)
        {
            return Ok(new ApiResponse<object, object>(userMFA, permissions));
        }

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

        return Ok(new ApiResponse<object, object>(userMFA, permissions));
    }

    [CustomAuthorize("member_update_mfa", "update", "userGuid")]
    [HttpPost("setup-authenticator")]
    [ProducesResponseType(typeof(ApiResponse<MFASetupResponseDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetupAuthenticatorAsync([FromBody] ResendCodeQueryParamByGuid queryParam, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(queryParam.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        MFASetupResponseDto userMFAResponse =
            queryParam.AuthChannel.ToLower() == "emailauth"
                ? await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = user.UserId, Email = queryParam.Args["email"] })
                : await _mfaRepository.SetupAuthenticator(queryParam.AuthChannel, queryParam.Args);

        await _mediator.Send(new SaveMFALogCommand
        {
            UserId = user.UserId,
            Type = queryParam.AuthChannel,
            Action = "SetupAuthenticator",
            Args = queryParam.Args,
            Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage }
        });

        if (userMFAResponse != null && userMFAResponse.IsSuccess && (queryParam.AuthChannel.ToLower() == "authapp" || queryParam.AuthChannel.ToLower() == "emailauth"))
        {
            var userMfa = new UserMFA
            {
                AuthenticatorKey = queryParam.AuthChannel.ToLower() == "authapp" ? userMFAResponse.SharedKey : "",
                WhatsAppNumber = queryParam.AuthChannel.ToLower() == "whatsapp" ? queryParam.Args["WhatsAppNumber"] : "",
                PhoneCode = queryParam.AuthChannel.ToLower() == "whatsapp" ? queryParam.Args["PhoneCode"] : "",
                CountryCode = queryParam.AuthChannel.ToLower() == "whatsapp" ? queryParam.Args["CountryCode"] : "",
                Email = queryParam.AuthChannel.ToLower() == "emailauth" ? queryParam.Args["email"] : ""
            };

            bool setupResult = await _mediator.Send(new SetUpMFACommand { UserId = user.UserId, AuthChannel = queryParam.AuthChannel, UserMFA = userMfa });
            if (!setupResult)
                return BadRequest(new ApiResponse<object, object>(null, 400, "Setup Authenticator Failed!", "error"));
        }

        return Ok(new ApiResponse<object, object>(userMFAResponse));
    }

    [CustomAuthorize]
    [HttpPost("verify-code")]
    [ProducesResponseType(typeof(ApiResponse<MFAVerifyDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyCodeAsync([FromBody] VerifyCodeQueryParamByUserGuid queryParam, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(queryParam.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        if (queryParam.Args == null || queryParam.Args.Count == 0)
            queryParam.Args = new Dictionary<string, string> { { "code", queryParam.Code } };

        MFAVerifyDto verifyResult = new MFAVerifyDto();
        if (queryParam.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase))
        {
            verifyResult = await _mediator.Send(new VerifyEmailOTPQuery { UserId = user.UserId, OTPCode = queryParam.Code });
        }
        else
        {
            var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(user.UserId));
            verifyResult = await _mfaRepository.VerifyCode(queryParam.AuthChannel, queryParam.Args, userMFA);
        }

        await _mediator.Send(new SaveMFALogCommand
        {
            UserId = user.UserId,
            Type = queryParam.AuthChannel,
            Action = queryParam.Method,
            Args = queryParam.Args,
            Obj = new { verifyResult }
        });

        queryParam.Code = "";
        queryParam.Args.Clear();

        if (verifyResult.IsValid)
        {
            bool enabled = await _mediator.Send(new EnableDisableMFACommand { UserId = user.UserId, UpdateFlag = true, AuthChannel = queryParam.AuthChannel });
            if (enabled)
            {
                var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(user.UserId));
                verifyResult.VerifyDate = queryParam.AuthChannel.Equals("authapp", StringComparison.OrdinalIgnoreCase)
                    ? userMFA.EnableAuthenticatorAppDate?.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.AppStartDateTimezoneName
                    : userMFA.EmailAuthEnableDate?.ToString("dd MMMM yyyy HH:mm") + " " + userMFA.EmailAuthEnableDateTimezoneName;

                if (queryParam.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase))
                    verifyResult.To = userMFA.Email;

                return Ok(new ApiResponse<object, object>(verifyResult, 200, "Code verification successful"));
            }
        }

        verifyResult.IsValid = false;
        return BadRequest(new ApiResponse<object, object>(verifyResult, 400, "Code verification failed"));
    }

    [CustomAuthorize("member_update_mfa", "update", "userGuid")]
    [HttpPost("remove-authenticator")]
    public async Task<IActionResult> RemoveAuthenticatorAsync([FromBody] RemoveAuthenticatorModel model, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(model.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        await _mediator.Send(new UpdateRememberDeviceCommand { UserId = user.UserId, IsAdmin = false });
        var result = await _mediator.Send(new RemoveAuthenticatorCommand
        {
            UserId = user.UserId,
            AuthChannel = model.AuthChannel
        });

        if (!result)
        {
            _error.CustomValidation<object>("Failed to remove authenticator!");
            return new EmptyResult();
        }

        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("send-otp")]
    [ProducesResponseType(typeof(ApiResponse<MFASetupResponseDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendOtpAsync([FromBody] VerifyMFALoginQuery query)
    {
        var isValidUser = await _mediator.Send(new ValidateUserQuery { UserName = query.UserName, Password = query.Password });
        if (!isValidUser)
        {
            _error.InvalidCredentials<object>("Invalid user.");
            return new EmptyResult();
        }

        var user = await _mediator.Send(query);
        if (user == null)
        {
            _error.NotFound<object>("User not found!.");
            return new EmptyResult();
        }

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

    [CustomAuthorize]
    [HttpPost("validate-user")]
    public async Task<IActionResult> ValidateUserAsync([FromBody] ValidateUserQuery query)
    {
        var result = await _mediator.Send(query);
        if (!result)
        {
            _error.InvalidCredentials<object>("Invalid user.");
            return new EmptyResult();
        }

        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("enable-disable-mfa-admin")]
    public async Task<IActionResult> EnableOrDisableMfaAdminAsync([FromBody] EnableDisableMFAForAdminModel model, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(model.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = user.MemberDocId, InvokingUserId = user.UserId }))
        {
            _error.Unauthorized<object>("Unauthorized to perform this operation!");
            return new EmptyResult();
        }

        await _mediator.Send(new UpdateRememberDeviceCommand { UserId = user.UserId, IsAdmin = true });
        var result = await _mediator.Send(new EnableDisableMFAForAdminCommand
        {
            MemberDocId = user.MemberDocId,
            UserId = user.UserId,
            AppUpdateFlag = model.AppUpdateFlag,
            WhatsAppUpdateFlag = model.WhatsAppUpdateFlag,
            ByPassForceMFASetUpFlag = model.ByPassForceMFASetUpFlag,
            EmailAuthFlag = model.EmailAuthFlag
        });
        await _mediator.Send(new SaveMFAMandatoryUserCommand { UserId = user.UserId, UpdateFlag = model.ByPassForceMFASetUpFlag });

        if (!result)
        {
            _error.CustomValidation<object>("Update failed!");
            return new EmptyResult();
        }

        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize("member_update_mfa", "update", "userGuid")]
    [HttpPost("enable-disable-mfa")]
    public async Task<IActionResult> EnableOrDisableMfaAsync([FromBody] EnableDisableMFAModel model, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(model.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(user.UserId));
        if (userMFA == null)
        {
            _error.Forbidden<object>("MFA not enabled for this user!");
            return new EmptyResult();
        }

        if ((userMFA.whatsAppState == 2 && model.AuthChannel.Equals("whatsapp", StringComparison.OrdinalIgnoreCase)) ||
            (userMFA.AuthenticatorAppState == 2 && model.AuthChannel.Equals("apthapp", StringComparison.OrdinalIgnoreCase)) ||
            (userMFA.EmailAuthState == 2 && model.AuthChannel.Equals("emailauth", StringComparison.OrdinalIgnoreCase)))
        {
            _error.CustomValidation<object>("Operation failed!");
            return new EmptyResult();
        }

        await _mediator.Send(new UpdateRememberDeviceCommand { UserId = user.UserId, IsAdmin = false });
        var result = await _mediator.Send(new EnableDisableMFACommand
        {
            UserId = user.UserId,
            AuthChannel = model.AuthChannel,
            UpdateFlag = model.UpdateFlag
        });

        if (!result)
        {
            _error.CustomValidation<object>("Operation failed!");
            return new EmptyResult();
        }

        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("admin-mfa-user")]
    [ProducesResponseType(typeof(ApiResponse<UserMFA, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminMFAUserAsync([FromBody] IsActionAllowQueryModel queryModel, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(queryModel.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = user.MemberDocId, InvokingUserId = user.UserId }))
        {
            _error.Unauthorized<object>("Unauthorized to perform this operation!");
            return new EmptyResult();
        }

        var userMFA = await _mediator.Send(new GetAdminUserMFAQuery(user.MemberDocId));
        if (userMFA == null)
        {
            _error.Forbidden<object>("MFA isn't enabled for this user!");
            return new EmptyResult();
        }

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

    [CustomAuthorize]
    [HttpGet("accounts/{guid:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<UserMFA, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMemberAccountsBySyncGuid([FromRoute] Guid guid, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserByUserSyncIdQuery(guid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

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

    [CustomAuthorize("member_update_mfa", "update", "userGuid")]
    [HttpGet("backup-code/{userGuid:guid:required}")]
    public async Task<IActionResult> GetBackupCodeAsync([FromRoute] Guid userGuid, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(userGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        var userMFA = await _mediator.Send(new GetMfaByUserIdQuery(user.UserId));
        if (userMFA == null)
        {
            _error.Forbidden<object>("MFA isn't enabled for this user!");
            return new EmptyResult();
        }

        string backupCode = _mfaRepository.GenerateBackupCode();
        bool isValidate = await _mediator.Send(new SaveBackupCodeCommand { UserId = user.UserId, BackupCode = backupCode });
        if (!isValidate)
        {
            _error.Forbidden<object>("MFA isn't enabled.");
            return new EmptyResult();
        }

        return Ok(new ApiResponse<object, object>(backupCode));
    }

    [CustomAuthorize]
    [HttpPost("resend-code")]
    [ProducesResponseType(typeof(ApiResponse<MFAVerifyDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendCodeAsync([FromBody] ResendCodeQueryParamByGuid queryParam, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(queryParam.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        var userMFAResponse = await _mediator.Send(new SetupEmailAuthenticatorCommand { UserId = user.UserId, Email = queryParam.Args["email"] });
        if (!userMFAResponse.IsSuccess)
        {
            _error.CustomValidation<object>("Failed to respond code!");
            return new EmptyResult();
        }

        await _mediator.Send(new SaveMFALogCommand
        {
            UserId = user.UserId,
            Type = queryParam.AuthChannel,
            Action = "ResendCode",
            Args = queryParam.Args,
            Obj = new { userMFAResponse.IsSuccess, userMFAResponse.StatusMessage }
        });

        var returnData = new MFAVerifyDto { IsValid = true, To = userMFAResponse.To, Message = userMFAResponse.StatusMessage };
        return Ok(new ApiResponse<object, object>(returnData, 200, userMFAResponse.StatusMessage));
    }

    [CustomAuthorize]
    [HttpGet("country-phone-code")]
    [ProducesResponseType(typeof(ApiResponse<IList<CountryCodes>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountryPhoneCodeAsync()
    {
        var result = await _mediator.Send(new GetCountryPhoneCodeQuery());
        if (!result.Any())
            return Ok(new ApiResponse<object, object>(null, 404, "No data found"));

        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("validate-mandatory-mfa-user/{userGuid:guid:required}")]
    public async Task<IActionResult> ValidateMandatoryMFAUserAsync([FromRoute] Guid userGuid, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(userGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        var mfaEnabled = await _systemSettingsService.GetSystemSettings("SYSTEM.MFA.ENABLETWOFACTORAUTH", cancellationToken, "GoMembership");
        if (!Convert.ToBoolean(mfaEnabled))
        {
            _error.Forbidden<object>("MFA isn't enabled.");
            return new EmptyResult();
        }

        var isMandatory = await _mediator.Send(new ValidateMFAMandatoryUserQuery(user.UserId));
        return Ok(new ApiResponse<object, object>(isMandatory));
    }

    [CustomAuthorize]
    [HttpPost("mandatory-mfa-user")]
    public async Task<IActionResult> GetMandatoryMFAUserAsync([FromBody] IsActionAllowQueryModel queryModel, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(queryModel.UserGuid), cancellationToken);
        if (user is null)
        {
            _error.NotFound<object>("User not found!");
            return new EmptyResult();
        }

        if (!await _mediator.Send(new IsActionAllowedQuery { MemberDocId = user.MemberDocId, InvokingUserId = user.UserId }))
        {
            _error.Unauthorized<object>("Unauthorized to perform this operation!");
            return new EmptyResult();
        }

        var isMandatory = await _mediator.Send(new GetMandatoryMFAUserDataQuery(user.MemberDocId));
        var statusCode = 200;
        var message = isMandatory ? "Successful" : "No data found";

        return Ok(new ApiResponse<object, object>(isMandatory, statusCode, message));
    }

}