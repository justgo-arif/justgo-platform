using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Email.Commands.SendNotificationEmail
{
    public class SendNotificationEmailCommandHandler : IRequestHandler<SendNotificationEmailCommand, bool>
    {

        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISystemSettingsService _systemSettingsService;

        public SendNotificationEmailCommandHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, ISystemSettingsService systemSettingsService)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<bool> Handle(SendNotificationEmailCommand request, CancellationToken cancellationToken)
        {
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            var systemurl = await _systemSettingsService.GetSystemSettingsByItemKey(
                "CLUBPLUS.CENTRALSYSTEMURL", cancellationToken);
            string body = @$"
                            <div style='background: #fff; padding: 16px; font-family: Arial, sans-serif; color: #333;'>
                              <h2 style='color: #444;'>Dear Concern,</h2>
                              <p>We noticed that your account does not have an active payment method on file. 
                              To ensure uninterrupted service and access to your plan, please add your card details.</p>

                              <div style='background: #f8f8f8; padding: 12px; border-radius: 6px; margin: 16px 0;'>
                                <strong>Why add a payment method?</strong>
                                <ul>
                                  <li>Secure and encrypted payment processing</li>
                                  <li>Automatic plan renewals without interruptions</li>
                                  <li>Ability to use premium features</li>
                                </ul>
                              </div>

                              <p>
                                Click the button below to add your card securely:
                              </p>
                              <p>
                                <a href='{systemurl}' 
                                   style='background: #4CAF50; color: white; padding: 10px 18px; text-decoration: none; border-radius: 5px;'>
                                   Add Payment Method
                                </a>
                              </p>

                              <p>If you have any questions, simply reply to this email and our support team will help you.</p>

                              <p style='margin-top: 24px;'>Thanks,<br>The JustGo Team</p>
                            </div>
            ";
            var parameters = new
            {
                Sender = request.Sender,
                To = request.Recipient,
                Subject = "Action Required: Add a Payment Method to Continue Your Plan",
                MailBody = body,
                AttachmentsPath = "",
                FailCount = 0,
                Status = 1,
                Tag = request.EmailId,
                OwnerId = request.OwningEntityId
            };

            var rowsAffected = await _writeRepository
          .GetLazyRepository<object>()
          .Value
          .ExecuteAsync(SqlQueries.MAILQUEUE_INSERT, cancellationToken, parameters, dbTransaction, "text");
            try
            {
                await _unitOfWork.CommitAsync(dbTransaction);
                return rowsAffected > 0;
            }
            catch
            {
                await _unitOfWork.RollbackAsync(dbTransaction);
                throw;
            }

        }
    }
}
