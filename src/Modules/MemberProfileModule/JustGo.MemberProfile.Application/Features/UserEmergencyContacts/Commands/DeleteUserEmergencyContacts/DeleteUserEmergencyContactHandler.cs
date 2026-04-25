using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.DeleteUserEmergencyContacts
{
    public class DeleteUserEmergencyContactHandler : IRequestHandler<DeleteUserEmergencyContactCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteUserEmergencyContactHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(DeleteUserEmergencyContactCommand request, CancellationToken cancellationToken)
        {

            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            const string query =
                """
                DECLARE @DeletedRowIds TABLE (RowId INT);

                DELETE FROM [dbo].[UserEmergencyContacts]
                OUTPUT deleted.RowId INTO @DeletedRowIds(RowId)
                WHERE RecordGuid = @RecordGuId;

                DELETE FROM [dbo].[Members_EmergencyContact]
                WHERE RowId IN (SELECT RowId FROM @DeletedRowIds)
                """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuId", request.Id.ToString());

            var affectedRows = await repo.ExecuteAsync(query, cancellationToken, queryParameters, transaction, "text");

            await _unitOfWork.CommitAsync(transaction);

            return affectedRows;

        }
    }


}
