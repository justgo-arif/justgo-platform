using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.DeleteView
{
    public class DeleteFinanceGridViewHandler : IRequestHandler<DeleteFinanceGridViewCommand, bool>
    {

        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteFinanceGridViewHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteFinanceGridViewCommand request, CancellationToken cancellationToken)
        {

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            const string sql = @"Delete FinanceGridView 
                                WHERE ViewId = @ViewId;
                                Delete FinanceGridViewPreference 
                                WHERE ViewId = @ViewId;";
            var parameters = new
            {
                ViewId = request.ViewId
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
