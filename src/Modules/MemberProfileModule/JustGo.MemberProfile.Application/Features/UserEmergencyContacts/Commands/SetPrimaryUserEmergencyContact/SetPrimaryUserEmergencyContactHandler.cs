using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.SetPrimaryUserEmergencyContact
{
    public class SetPrimaryUserEmergencyContactHandler : IRequestHandler<SetPrimaryUserEmergencyContactCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public SetPrimaryUserEmergencyContactHandler(
            IWriteRepositoryFactory writeRepositoryFactory,
            IUnitOfWork unitOfWork, IUtilityService utilityService)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<int> Handle(SetPrimaryUserEmergencyContactCommand request, CancellationToken cancellationToken)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<UserEmergencyContact>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            int userId = await _utilityService.GetCurrentUserId(cancellationToken);
            if (userId == 0)
                return 0;

            string unsetSql = @"UPDATE [dbo].[UserEmergencyContacts]
                            SET IsPrimary = 0
                            WHERE UserId = @UserId";

            string setSql = @"UPDATE [dbo].[UserEmergencyContacts]
                          SET IsPrimary = 1
                          WHERE Id = @ContactId AND UserId = @UserId";

            await repo.ExecuteAsync(unsetSql, cancellationToken, new { UserId = userId }, transaction, "Text");
            var affected = await repo.ExecuteAsync(setSql, cancellationToken, new { ContactId = request.ContactId, UserId = userId }, transaction, "Text");

            await _unitOfWork.CommitAsync(transaction);

            return affected;
        }

    }
}
