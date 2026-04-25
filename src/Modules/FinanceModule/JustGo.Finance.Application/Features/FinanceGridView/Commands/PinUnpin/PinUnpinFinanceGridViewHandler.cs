using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.PinUnpin
{
    public class PinUnpinFinanceGridViewHandler : IRequestHandler<PinUnpinFinanceGridViewCommand, bool>
    {

        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public PinUnpinFinanceGridViewHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IUtilityService utilityService)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(PinUnpinFinanceGridViewCommand request, CancellationToken cancellationToken)
        {

            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            const string sql = @"UPDATE FinanceGridViewPreference
                                SET IsPinned = @IsPinned,
                                    UpdatedBy = @UserId,     
                                    UpdatedAt = GETDATE()
                                WHERE UserId = @UserId AND ViewId = @ViewId;";
            var parameters = new
            {
                ViewId = request.ViewId,
                UserId = currentUserId,
                IsPinned = request.IsPinned
            };
            var rowsAffected = await _writeRepository
           .GetLazyRepository<object>()
           .Value
           .ExecuteAsync(sql, cancellationToken, parameters, dbTransaction, "text");
            await _unitOfWork.CommitAsync(dbTransaction);
            return rowsAffected > 0;

        }
    }
}
