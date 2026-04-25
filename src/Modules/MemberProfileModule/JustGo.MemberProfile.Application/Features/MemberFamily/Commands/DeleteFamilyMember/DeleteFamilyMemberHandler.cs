using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.DeleteFamilyMember;

public class DeleteFamilyMemberHandler : IRequestHandler<DeleteFamilyMemberCommand, string>
{

    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFamilyMemberHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(DeleteFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
        using var transaction = await _unitOfWork.BeginTransactionAsync();

        const string sql =
        """
        DECLARE @FamilyRowId INT
        DECLARE @MemberRowId INT

        SELECT TOP 1 @MemberRowId=ml.RowId,@FamilyRowId=fl.RowId 
        FROM UserFamilies uf
        INNER JOIN Families f on f.FamilyId=uf.FamilyId
        INNER JOIN Document d on d.SyncGuid=f.RecordGuid
        INNER JOIN [User] u on u.UserId=uf.UserId
        LEFT JOIN Family_Links fl on fl.DocId=d.DocId AND fl.EntityId=u.MemberDocId
        LEFT JOIN Members_Links ml on ml.DocId=u.MemberDocId AND ml.EntityId=d.DocId
        WHERE uf.UserFamilyId=@UserFamilyId

        DELETE FROM Members_Links WHERE RowId=ISNULL(@MemberRowId,0)
        DELETE FROM Family_Links WHERE RowId=ISNULL(@FamilyRowId,0) 
        DELETE FROM UserFamilies WHERE UserFamilyId=@UserFamilyId
        """;

        var parameters = new DynamicParameters();
        parameters.Add("@UserFamilyId", request.UserFamilyId, dbType: DbType.Int32);

        var affected = await repo.ExecuteAsync(sql, cancellationToken, parameters, transaction, "text");

        await _unitOfWork.CommitAsync(transaction);

        return "Success";
    }
}
