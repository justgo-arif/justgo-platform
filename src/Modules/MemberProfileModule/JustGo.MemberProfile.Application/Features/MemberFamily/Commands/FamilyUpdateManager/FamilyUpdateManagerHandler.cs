using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.MemberFamily.Commands.FamilyUpdateManager;
using System.Data;


namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.FacilityRequestAction;

public class FamilyUpdateManagerHandler : IRequestHandler<FamilyUpdateManagerCommand, int>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    public FamilyUpdateManagerHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(FamilyUpdateManagerCommand request, CancellationToken cancellationToken)
    {
        var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;

        using var transaction = await _unitOfWork.BeginTransactionAsync();

        const string sql =
            """
            DECLARE @FamilyManagerRoleId INT;
            SELECT TOP 1 @FamilyManagerRoleId = Id FROM AbacRoles WHERE [Name] = 'Family Manager';

            UPDATE UserFamilies SET [IsAdmin] = @MakeManager WHERE RecordGuid = @RecordGuid

            IF (@MakeManager=1)
            BEGIN
                INSERT INTO AbacUserRoles (UserId,RoleId,OrganizationId)
                SELECT UserId,@FamilyManagerRoleId,0 FROM UserFamilies uf WHERE IsAdmin=1 AND uf.RecordGuid = @RecordGuid
                AND NOT EXISTS (SELECT 1 FROM AbacUserRoles WHERE UserId=uf.UserId AND RoleId=@FamilyManagerRoleId)
            END

            IF (@MakeManager=0)
            BEGIN
                 DELETE aur FROM AbacUserRoles aur INNER JOIN UserFamilies uf ON uf.UserId = aur.UserId
                 WHERE uf.RecordGuid = @RecordGuid AND aur.RoleId = @FamilyManagerRoleId
                 AND NOT EXISTS (SELECT 1 FROM UserFamilies uf2 WHERE uf2.UserId = uf.UserId AND uf2.IsAdmin = 1);
            END
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@RecordGuid", request.RecordGuid, DbType.Guid);
        parameters.Add("@MakeManager", request.MakeManager ? 1 : 0, DbType.Int32);

        var insertedId = await repo.ExecuteAsync(sql, cancellationToken, parameters, transaction, "Text");

        await _unitOfWork.CommitAsync(transaction);

        return insertedId;
    }
}