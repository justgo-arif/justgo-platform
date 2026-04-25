using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;


namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.AddFamilyMember;

public class AddFamilyMemberHandler : IRequestHandler<AddFamilyMemberCommand, int>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;


    public AddFamilyMemberHandler(
        IWriteRepositoryFactory writeRepositoryFactory,
        IUnitOfWork unitOfWork,
        IUtilityService utilityService)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<int> Handle(AddFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var repo = _writeRepositoryFactory.GetLazyRepository<FamilyMember>().Value;

        using var transaction = await _unitOfWork.BeginTransactionAsync();

        string sql = """
        DECLARE @MemberDocId INT =(SELECT TOP 1 MemberDocId FROM [User] WHERE UserSyncId=@MemberSyncGuid) 

        DECLARE @SiteAddress NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey = 'SYSTEM.SITEADDRESS')
        DECLARE @URL NVARCHAR(MAX)=@SiteAddress+'/Workbench/i/r/My-Profile/'+LOWER(@MemberSyncGuid)+'/family-details'

        DECLARE @RepositoryId INT =(SELECT TOP 1 RepositoryId FROM Repository WHERE [Name]='Family')

        DECLARE @FamilyGuid NVARCHAR(36)=(SELECT TOP 1 f.RecordGuid FROM Families f INNER JOIN UserFamilies uf ON uf.FamilyId =f.FamilyId 
        INNER JOIN [User] u on u.UserId=uf.UserId WHERE u.UserSyncId= @UserSyncGuid)

        IF (@FamilyGuid IS NULL)
        BEGIN
             DECLARE @DocId INT
             DECLARE @FamilyName NVARCHAR(500)= (SELECT TOP 1 FirstName+'''s Family' FROM [User] WHERE UserSyncId=@UserSyncGuid)

             INSERT INTO Document(RepositoryId,Type,Title,RegisterDate,[Location],IsLocked,[Status],Tag,[Version],userId)
             SELECT @RepositoryId,'Electronic','Member Family',GETDATE(),'Virtual',0,0,0,1,UserId 
             FROM [User] WHERE UserId=@ActionUserId
             SET @DocId=@@IDENTITY
        
             INSERT INTO Document_23_136(DocId, [Version])
             VALUES (@DocId,1) 
        
             EXEC SetAutoId @DocId
        
             UPDATE Family_Default SET Familyname=@FamilyName WHERE DocId=@DocId

             INSERT INTO Families (Reference,FamilyName,RegisterDate,CreatedBy,RecordGuid)
             SELECT TOP 1 fd.Reference,fd.FamilyName,d.RegisterDate,d.[UserId],d.SyncGuid
             FROM Family_Default fd INNER JOIN Document d on d.DocId=fd.DocId
             WHERE d.DocId=@DocId

             SELECT @FamilyGuid=SyncGuid FROM Document WHERE DocId= @DocId

             INSERT INTO [UserFamilies] (FamilyId,UserId,IsAdmin,[Status],JoinDate)
             SELECT TOP 1 f.FamilyId,u.Userid,1,1,GETDATE() FROM Families f
             INNER JOIN [User] u on u.UserSyncId=@UserSyncGuid WHERE f.RecordGuid=@FamilyGuid

             INSERT INTO Family_Links (DocId,EntityId,Entityparentid,Title)
             SELECT d.DocId,u.MemberDocId,1,'Family-Member Link' FROM Document d 
             INNER JOIN [User] u on u.UserSyncId=@UserSyncGuid WHERE d.SyncGuid=@FamilyGuid

             INSERT INTO Members_Links (DocId,EntityId,Entityparentid,Title)
             SELECT u.MemberDocId,d.DocId,@RepositoryId,'Member-Family Link' FROM Document d 
             INNER JOIN [User] u on u.UserSyncId=@UserSyncGuid WHERE d.SyncGuid=@FamilyGuid

             INSERT INTO [AbacUserRoles] (UserId,RoleId,OrganizationId)
             SELECT u.UserId,ar.Id,0 FROM AbacRoles ar INNER JOIN [User] u ON u.UserSyncId=@UserSyncGuid
             WHERE [Name] ='Family Manager' AND NOT EXISTS (SELECT 1 FROM [AbacUserRoles] WHERE UserId=u.UserId AND RoleId=ar.Id)

        END

        INSERT INTO [UserFamilies] (FamilyId,UserId,IsAdmin,[Status],JoinDate)
        SELECT TOP 1 f.FamilyId,u.Userid,0,CAST(@IsNewMember AS int),GETDATE() FROM Families f
        INNER JOIN [User] u on u.UserSyncId=@MemberSyncGuid WHERE f.RecordGuid=@FamilyGuid

        IF(@IsNewMember=1)
        BEGIN
            INSERT INTO Family_Links (DocId,EntityId,Entityparentid,Title)
            SELECT d.DocId,u.MemberDocId,1,'Family-Member Link' FROM Document d 
            INNER JOIN [User] u on u.UserSyncId=@MemberSyncGuid WHERE d.SyncGuid=@FamilyGuid
        
            INSERT INTO Members_Links (DocId,EntityId,Entityparentid,Title)
            SELECT u.MemberDocId,d.DocId,@RepositoryId,'Member-Family Link' FROM Document d 
            INNER JOIN [User] u on u.UserSyncId=@MemberSyncGuid WHERE d.SyncGuid=@FamilyGuid
        END

        IF(@LinkToClubs=1)
        BEGIN
           EXEC SaveAndLinkFamilyClubMember @UserSyncGuid,@MemberSyncGuid,@ActionUserId
        END

        IF(@IsNewMember=0)
        BEGIN
            EXEC SEND_EMAIL_BY_SCHEME @MessageScheme='Family\Link Request',@ForEntityId=@MemberDocId
            ,@GetInfo=0,@InvokeUserId=@ActionUserId,@OwnerType='NGB',@OwnerId=0,@Argument=@URL
        END

        """;

        var currentUser = await _utilityService.GetCurrentUserPublic(cancellationToken);
        if (currentUser == null)
        {
            throw new InvalidOperationException("Current user cannot be null.");
        }

        var insertParameters = new DynamicParameters();
        insertParameters.Add("@MemberSyncGuid", request.MemberSyncGuid);
        insertParameters.Add("@UserSyncGuid", request.UserSyncGuid);
        insertParameters.Add("@ActionUserId", currentUser.UserId);
        insertParameters.Add("@IsNewMember", request.IsNewMember);
        insertParameters.Add("@LinkToClubs", request.LinkToClubs);

        var insertedId = await repo.ExecuteAsync(sql, cancellationToken, insertParameters, transaction, "Text");

        await _unitOfWork.CommitAsync(transaction);

        return insertedId;
    }
}
