using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.SendVerificationMail;

public class SendVerificationMailHandler : IRequestHandler<SendVerificationMailCommand, OperationResultDto>
{
    private readonly LazyService<IReadRepository<object>> _readRepository;
    private readonly LazyService<IWriteRepository<object>> _writeRepository;
    private readonly IMediator _mediator;
    private readonly ISystemSettingsService _systemSettingsService;
    IUnitOfWork _unitOfWork;

    public SendVerificationMailHandler(
        LazyService<IReadRepository<object>> readRepository,
        LazyService<IWriteRepository<object>> writeRepository,
        IMediator mediator,
        ISystemSettingsService systemSettingsService,
        IUnitOfWork unitOfWork)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _mediator = mediator;
        _systemSettingsService = systemSettingsService;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResultDto> Handle(SendVerificationMailCommand request, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserByUserSyncIdQuery(request.UserSyncId), cancellationToken);
        if (user == null || user.Userid <= 0)
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = "Member not found.",
                RowsAffected = 0
            };

        return await VerificationActivity(request, user, cancellationToken);
    }

    private async Task<OperationResultDto> VerificationActivity(SendVerificationMailCommand request, User user, CancellationToken cancellationToken)
    {
        var url = await GetSiteUrl(cancellationToken);

        using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var tokenId = await CreateActionToken("EmailVerification", user.Userid, cancellationToken);
            var token = GenerateToken(user.LoginId, request.Type, tokenId + user.Userid, user.EmailAddress);

            var redirectUrl = url + "Account.mvc/Feedback?feedbackType=error&&message=";
            var redirectFeedbackUrl = url + "Account.mvc/Feedback?feedbackType=info&&message=" + GetFeedbackMessage(request.Type);
            var approverUrl = url + "ActionToken.mvc/Invoke?token=" + token;

            await SaveTokenAndArguments(tokenId, token, user.Userid, request.Type, redirectUrl, redirectFeedbackUrl, cancellationToken);
            await SendNotificationEmails(request.Type, user, approverUrl, cancellationToken);

            await _unitOfWork.CommitAsync(dbTransaction);

            return new OperationResultDto
            {
                IsSuccess = true,
                Message = "Verification email sent.",
                RowsAffected = 1
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(dbTransaction);
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = ex.Message,
                RowsAffected = 0
            };
        }
    }

    private async Task<int> CreateActionToken(string handlerType, int userId, CancellationToken cancellationToken)
    {
        var sql = """
            DECLARE @ExistingTokenId INT;

            SELECT TOP 1 @ExistingTokenId = Id
            FROM ActionToken A
            INNER JOIN ActionTokenHandlerArguments ARG ON ARG.ActionTokenId = A.Id
            WHERE ARG.[Name] = 'UserId' AND ARG.[Value] = @UserId
            AND A.HandlerType = @HandlerType
            AND DATEDIFF(MINUTE, A.CreateDate, GETUTCDATE()) <= 10
            ORDER BY A.CreateDate DESC;

            IF @ExistingTokenId IS NULL
            BEGIN
                INSERT INTO ActionToken (HandlerType, CreateDate, [Status], VaildFor) 
                OUTPUT INSERTED.Id 
                VALUES (@HandlerType, GETUTCDATE(), 0, 200000);
            END
            ELSE
            BEGIN
            	THROW 50000, 'The email has already sent.', 1;
            END
            """;
        var result = await _readRepository.Value.GetSingleAsync(sql, cancellationToken, new { HandlerType = handlerType, UserId = userId.ToString() }, null, "text");
        return result as dynamic;
    }

    private static string GenerateToken(string loginId, string handler, int value, string email)
    {
        var baseString = $"{loginId}{handler}{value}{email}";
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private static string GetFeedbackMessage(string type) => type switch
    {
        "ParentalApproval" => "Thank you. We acknowledge your Parental Consent.",
        "TwoFactor" => "Thank you. Your account is now verified.",
        _ => "Thank you."
    };

    private async Task SaveTokenAndArguments(int id, string token, int userId, string handler, string redirectUrl, string feedbackUrl, CancellationToken cancellationToken)
    {
        var sql = """
                UPDATE ActionToken SET Token = @Token WHERE Id = @Id;

                INSERT INTO ActionTokenHandlerArguments(ActionTokenId, Name, Value) 
                VALUES 
                    (@Id, 'UserId', @UserId),
                    (@Id, 'Handler', @Handler),
                    (@Id, 'RedirectUrl', @RedirectUrl),
                    (@Id, 'RedirectFeedBackUrl', @RedirectFeedBackUrl);
            """;

        var param = new DynamicParameters();
        param.Add("@Id", id, DbType.Int32);
        param.Add("@Token", token, DbType.String);
        param.Add("@UserId", userId.ToString(), DbType.String);
        param.Add("@Handler", handler, DbType.String);
        param.Add("@RedirectUrl", redirectUrl, DbType.String);
        param.Add("@RedirectFeedBackUrl", feedbackUrl, DbType.String);

        await _writeRepository.Value.ExecuteAsync(sql, cancellationToken, param, null, "text");
    }

    private async Task SendNotificationEmails(string handler, User user, string approverUrl, CancellationToken cancellationToken)
    {
        var param = new DynamicParameters();
        param.Add("UserId", user.Userid);
        param.Add("URL", approverUrl);
        await _writeRepository.Value.ExecuteAsync($"[Send{handler}NotificationEmail]", cancellationToken, param);

        var mailParams = new DynamicParameters();
        mailParams.Add("@ForEntityId", user.Userid);
        mailParams.Add("@GetInfo", 0);
        mailParams.Add("@OwnerType", "NGB");
        mailParams.Add("@OwnerId", 0);
        mailParams.Add("@Argument", approverUrl);

        if (handler.ToLower() == "parentalapproval")
        {
            mailParams.Add("@MessageScheme", "Account\\Parental Approval");
            await _writeRepository.Value.ExecuteAsync("SEND_EMAIL_BY_SCHEME", cancellationToken, mailParams);
        }
        else if (handler.ToLower() == "twofactor")
        {
            mailParams.Add("@MessageScheme", user.SourceLocation == "Family" ? "Account\\Registration(Self)" : $"Account\\Registration({user.SourceLocation})");
            await _writeRepository.Value.ExecuteAsync("SEND_EMAIL_BY_SCHEME", cancellationToken, mailParams);
        }
    }

    private async Task<string> GetSiteUrl(CancellationToken cancellationToken)
    {
        var result = await _systemSettingsService.GetSystemSettingsByItemKey("SYSTEM.SITEADDRESS", cancellationToken);
        if (string.IsNullOrWhiteSpace(result))
            return "/";

        return result.EndsWith("/") ? result : result + "/";
    }
}
