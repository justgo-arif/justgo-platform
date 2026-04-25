using System.Data;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.UpdateUserEmergencyContacts
{
    public class UpdateUserEmergencyContactHandler : IRequestHandler<UpdateUserEmergencyContactCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserEmergencyContactHandler(
            IWriteRepositoryFactory writeRepositoryFactory,
            IUnitOfWork unitOfWork)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(UpdateUserEmergencyContactCommand request, CancellationToken cancellationToken)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<UserEmergencyContact>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            string baseUpdateSql =
                """
                DECLARE @RowIds TABLE (RowId INT)

                UPDATE [dbo].[UserEmergencyContacts] SET
                FirstName = @FirstName
                ,LastName = @LastName
                ,Relation = @Relation
                ,ContactNumber = @ContactNumber
                ,EmailAddress = @EmailAddress
                ,IsPrimary = @IsPrimary
                ,CountryCode = @CountryCode
                OUTPUT inserted.RowId INTO @RowIds (RowId)
                WHERE RecordGuid = @SyncGuid;

                UPDATE Members_EmergencyContact SET 
                 FirstName = @FirstName
                ,Surname = @LastName
                ,Relation = @Relation
                ,ContactNumber = @ContactNumber
                ,EmailAddress = @EmailAddress
                WHERE RowId IN (SELECT RowId FROM @RowIds)

                """;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@FirstName", request.FirstName, DbType.String);
            queryParameters.Add("@LastName", request.LastName, DbType.String);
            queryParameters.Add("@Relation", request.Relation, DbType.String);
            queryParameters.Add("@ContactNumber", request.ContactNumber, DbType.String);
            queryParameters.Add("@EmailAddress", request.EmailAddress, DbType.String);
            queryParameters.Add("@IsPrimary", request.IsPrimary ?? false, DbType.Boolean);
            queryParameters.Add("@CountryCode", request.CountryCode, DbType.String);
            queryParameters.Add("@SyncGuid", request.SyncGuid, DbType.String);

            int totalAffected = 0;

            var affectedRows = await repo.ExecuteAsync(baseUpdateSql, cancellationToken, queryParameters, transaction, "Text");
            totalAffected += affectedRows;
            /*
            if (request.IsPrimary.HasValue)
            {
                if (request.IsPrimary.Value)
                {
                    string unsetSql = @"
                UPDATE [dbo].[UserEmergencyContacts]
                SET IsPrimary = 0
                WHERE UserId = @UserId";

                    string setSql = @"
                UPDATE [dbo].[UserEmergencyContacts]
                SET IsPrimary = 1
                WHERE Id = @ContactId AND UserId = @UserId";

                    totalAffected += await repo.ExecuteAsync(unsetSql, cancellationToken, new { UserId = request.UserId }, transaction, "Text");
                    totalAffected += await repo.ExecuteAsync(setSql, cancellationToken, new { ContactId = request.Id, UserId = request.UserId }, transaction, "Text");
                }
                else
                {
                    string setSql = @"
                UPDATE [dbo].[UserEmergencyContacts]
                SET IsPrimary = 0
                WHERE Id = @ContactId AND UserId = @UserId";

                    totalAffected += await repo.ExecuteAsync(setSql, cancellationToken, new { ContactId = request.Id, UserId = request.UserId }, transaction, "Text");
                }
            }
            */
            await _unitOfWork.CommitAsync(transaction);

            return totalAffected;
        }

    }
}
