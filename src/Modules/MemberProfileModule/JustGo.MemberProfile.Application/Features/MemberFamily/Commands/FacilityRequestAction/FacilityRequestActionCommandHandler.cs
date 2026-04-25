using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;


namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.FacilityRequestAction;

public class FamilyRequestActionCommandHandler : IRequestHandler<FamilyRequestActionCommand, int>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    public FamilyRequestActionCommandHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(FamilyRequestActionCommand request, CancellationToken cancellationToken)
    {
        var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;

        using var transaction = await _unitOfWork.BeginTransactionAsync();

        const string sql =
            """
                IF (@StatusId=1)
                BEGIN
                    UPDATE UserFamilies SET [Status] = 1 WHERE RecordGuid = @RecordGuid

                    INSERT INTO Family_Links (DocId,EntityId,Entityparentid,Title)
                    SELECT d.DocId,u.MemberDocId,1,'Family-Member Link' FROM UserFamilies uf
                    INNER JOIN [User] u on u.UserId=uf.UserId
                    INNER JOIN Families f on f.FamilyId=uf.FamilyId
                    INNER JOIN Document d on d.SyncGuid=f.RecordGuid
                    WHERE uf.RecordGuid=@RecordGuid
                    AND NOT EXISTS (SELECT 1 FROM Family_Links WHERE DocId=d.DocId AND EntityId=u.MemberDocId) 
                    
                    INSERT INTO Members_Links (DocId,EntityId,Entityparentid,Title)
                    SELECT u.MemberDocId,d.DocId,d.RepositoryId,'Member-Family Link' FROM UserFamilies uf
                    INNER JOIN [User] u on u.UserId=uf.UserId
                    INNER JOIN Families f on f.FamilyId=uf.FamilyId
                    INNER JOIN Document d on d.SyncGuid=f.RecordGuid
                    WHERE uf.RecordGuid=@RecordGuid
                    AND NOT EXISTS (SELECT 1 FROM Members_Links WHERE EntityId=d.DocId AND DocId=u.MemberDocId) 
                END
                
                IF (@StatusId=2)
                BEGIN
                   DECLARE @FamilyRowId INT
                   DECLARE @MemberRowId INT
                
                   SELECT TOP 1 @MemberRowId=ml.RowId,@FamilyRowId=fl.RowId 
                   FROM UserFamilies uf
                   INNER JOIN Families f on f.FamilyId=uf.FamilyId
                   INNER JOIN Document d on d.SyncGuid=f.RecordGuid
                   INNER JOIN [User] u on u.UserId=uf.UserId
                   LEFT JOIN Family_Links fl on fl.DocId=d.DocId AND fl.EntityId=u.MemberDocId
                   LEFT JOIN Members_Links ml on ml.DocId=u.MemberDocId AND ml.EntityId=d.DocId
                   WHERE uf.RecordGuid=@RecordGuid
                
                   DELETE FROM Members_Links WHERE RowId=ISNULL(@MemberRowId,0)
                   DELETE FROM Family_Links WHERE RowId=ISNULL(@FamilyRowId,0) 
                   DELETE FROM UserFamilies WHERE RecordGuid=@RecordGuid
                END
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@RecordGuid", request.RecordGuid, DbType.Guid);
        parameters.Add("@StatusId", request.Accepted ? 1 : 2, DbType.Int32);

        var insertedId = await repo.ExecuteAsync(sql, cancellationToken, parameters, transaction, "Text");

        await _unitOfWork.CommitAsync(transaction);

        return insertedId;
    }
}