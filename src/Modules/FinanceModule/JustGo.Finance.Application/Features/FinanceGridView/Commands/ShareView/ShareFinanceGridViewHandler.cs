using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.ShareView
{
    public class ShareFinanceGridViewHandler : IRequestHandler<ShareFinanceGridViewCommand, bool>
    {

        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public ShareFinanceGridViewHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IUtilityService utilityService)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(ShareFinanceGridViewCommand request, CancellationToken cancellationToken)
        {

            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            const string sql = @"UPDATE FinanceGridView
                                SET IsShared = @IsShared,
                                    UpdatedBy = @UserId,     
                                    UpdatedAt = GETDATE()
                                WHERE  ViewId = @ViewId;";
            var parameters = new
            {
                ViewId = request.ViewId,
                UserId = currentUserId,
                IsShared = request.IsShared 
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
