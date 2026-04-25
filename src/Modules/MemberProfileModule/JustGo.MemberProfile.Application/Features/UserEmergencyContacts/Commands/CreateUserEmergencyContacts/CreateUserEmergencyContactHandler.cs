using System.Data;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.CreateUserEmergencyContacts;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Domain.Entities;

public class CreateUserEmergencyContactHandler : IRequestHandler<CreateUserEmergencyContactCommand, int>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserEmergencyContactHandler(
        IWriteRepositoryFactory writeRepositoryFactory,
        IUnitOfWork unitOfWork)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork; 
    }

    public async Task<int> Handle(CreateUserEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        var repo = _writeRepositoryFactory.GetLazyRepository<UserEmergencyContact>().Value;

        using var transaction = await _unitOfWork.BeginTransactionAsync();

        string sql = """
            INSERT INTO Members_EmergencyContact (DocId,Firstname,Surname,Relation,ContactNumber,EmailAddress)
            SELECT MemberDocId,@FirstName,@LastName,@Relation,@ContactNumber,@EmailAddress FROM [User] WHERE UserSyncId=@UserSyncGuid

            DECLARE @RowId INT=@@IDENTITY

            INSERT INTO [dbo].[UserEmergencyContacts] ([UserId],[FirstName],[LastName],[Relation],[ContactNumber],[EmailAddress],[IsPrimary],[CountryCode],[RowId])
            SELECT UserId,@FirstName,@LastName,@Relation,@ContactNumber,@EmailAddress,@IsPrimary,(CASE WHEN @CountryCode='UK' THEN 'GB' ELSE @CountryCode END),@RowId
            FROM [User] WHERE UserSyncId=@UserSyncGuid
            """;

        var insertParameters = new DynamicParameters();
        insertParameters.Add("@UserSyncGuid", request.UserSyncGuid, DbType.Guid);
        insertParameters.Add("@FirstName", request.FirstName);
        insertParameters.Add("@LastName", request.LastName);
        insertParameters.Add("@Relation", request.Relation);
        insertParameters.Add("@ContactNumber", request.ContactNumber);
        insertParameters.Add("@EmailAddress", request.EmailAddress);
        insertParameters.Add("@IsPrimary", request.IsPrimary ?? false, DbType.Boolean);
        insertParameters.Add("@CountryCode", request.CountryCode);

        var insertedId = await repo.ExecuteAsync(sql, cancellationToken, insertParameters, transaction, "Text");

        await _unitOfWork.CommitAsync(transaction);

        return insertedId;
    }
}
