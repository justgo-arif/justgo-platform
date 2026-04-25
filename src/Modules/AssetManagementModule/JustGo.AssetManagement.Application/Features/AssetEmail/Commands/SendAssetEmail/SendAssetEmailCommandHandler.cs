using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail
{
    public class SendAssetEmailCommandHandler : IRequestHandler<SendAssetEmailCommand, bool>
    {
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SendAssetEmailCommandHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(SendAssetEmailCommand command, CancellationToken cancellationToken)
        {
            var sendemail = "[dbo].[SEND_EMAIL_BY_SCHEME]";
            var emailparameters = new
            {
                MessageScheme = command.MessageScheme,
                Argument = command.Argument,
                ForEntityId = command.ForEntityId,
                TypeEntityId = command.TypeEntityId,
                InvokeUserId = command.InvokeUserId,
                OwnerType = command.OwnerId > 0 ? "Club" : "NGB",
                OwnerId = command.OwnerId,
                TestEmailAddress = "N/A",
                GetInfo = 0,
                MessageDocId = -1
            };
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                sendemail,
                cancellationToken,
                emailparameters,
                dbTransaction
            );
            await _unitOfWork.CommitAsync(dbTransaction);
            return true;
        }
    }

}
