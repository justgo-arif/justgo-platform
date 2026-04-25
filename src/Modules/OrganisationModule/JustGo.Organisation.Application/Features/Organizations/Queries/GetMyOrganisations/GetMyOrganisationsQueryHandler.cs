using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Organisation.Application.DTOs;
using JustGo.Organisation.Domain.Entities;
using System.Data;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetMyOrganisations;

public class GetMyOrganisationsQueryHandler : IRequestHandler<GetMyOrganisationsQuery, List<MyOrganisationDto>>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetMyOrganisationsQueryHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<List<MyOrganisationDto>> Handle(GetMyOrganisationsQuery request, CancellationToken cancellationToken)
    {
        var data = await GetMyOrganisationsAsync(request, cancellationToken);
        return data.Select(d => MapToDto(d)).ToList();
    }

    private async Task<List<MyOrganisation>> GetMyOrganisationsAsync(GetMyOrganisationsQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserSyncId", request.UserSyncId);

        string sql = """
        DECLARE @MaxLevelHierarchy INT = (SELECT MAX(LevelNo) FROM HierarchyTypes);

        WITH JoinedOrgs AS (
        	SELECT U.MemberDocId, H.[HierarchyId], H.HierarchyTypeId, cmd.DocId ClubMemberDocId,
        	CD.DocId ClubDocId, d.SyncGuid ClubSyncGuid, CD.ClubName, CD.ClubID, CD.LatLng, CD.[Location] [Image], CD.ClubemailAddress EmailAddress, CD.ClubWebSite
        	FROM [User] U
        	INNER JOIN Members_links ml on ml.docid = U.MemberDocId AND Entityparentid = 3
        	INNER JOIN clubmembers_default cmd on cmd.docid = ml.entityid
        	INNER JOIN clubmembers_links cml on cml.docid = cmd.docid AND cml.Entityparentid = 2
        	INNER JOIN Clubs_Default cd on cd.Docid = cml.entityId
        	INNER JOIN Hierarchies H ON H.EntityId = CD.DocId
        	INNER JOIN Document d on d.DocId = h.EntityId
        	WHERE U.UserSyncId = @UserSyncId
        ),
        JoinedClubs AS (
        	SELECT JO.ClubDocId
        	FROM JoinedOrgs JO
        	INNER JOIN HierarchyTypes T ON T.Id = JO.HierarchyTypeId AND T.LevelNo = @MaxLevelHierarchy
        ),
        CLUB_MEMBER_INFO AS (
        	SELECT CMR.ClubDocId, S.[Name] MemberStatus, CMR.RoleName, CMR.IsPrimary
            FROM ClubMemberRoles CMR
            INNER JOIN ProcessInfo p on p.PrimaryDocId=CMR.ClubMemberDocId
            INNER JOIN [State] S on s.StateId=p.CurrentStateId
            INNER JOIN JoinedOrgs JO ON JO.ClubDocId = CMR.ClubDocId
            INNER JOIN [User] U ON U.UserId = CMR.UserId AND U.UserSyncId = @UserSyncId
        ),
        CLUB_MEMBER_STATUS AS (
            SELECT CM.ClubDocId, 
            (SELECT STRING_AGG(MemberStatus, ',') FROM (SELECT DISTINCT MemberStatus FROM CLUB_MEMBER_INFO CMI WHERE CMI.ClubDocId = CM.ClubDocId) AS DS) AS MemberStatus,
            (SELECT STRING_AGG(RoleName, ',') FROM (SELECT DISTINCT RoleName FROM CLUB_MEMBER_INFO CMI WHERE CMI.ClubDocId = CM.ClubDocId) AS DR) AS Roles,
            MAX(CAST(CM.IsPrimary AS INT)) IsPrimary
        	FROM CLUB_MEMBER_INFO CM
            GROUP BY CM.ClubDocId
        ),
        TransferSummary AS (
        	SELECT DISTINCT cms.ClubDocId, td.DocId TransferDocId, td.Tempnewclubname, s.[Name] TransferStatus,d.SyncGuid TransferSyncGuid 
        	FROM JoinedOrgs cms
        	INNER JOIN Transfers_Default td on td.FromClubId=cms.ClubDocId
        	INNER JOIN Transfers_Links tl on tl.Docid=td.DocId AND tl.EntityId=cms.MemberDocId
        	INNER JOIN ProcessInfo p ON p.PrimaryDocId = td.DocId  --Need process Id
        	INNER JOIN [State] s ON s.StateId = p.CurrentStateId
            INNER JOIN Document d on d.DocId=td.DocId
        	WHERE s.Name IN ('Awaiting Approval NGB', 'Awaiting Approval Current Club', 'Awaiting Approval New Club')
        ),
        ParentChain AS (
        	SELECT JC.ClubDocId, STRING_AGG(CONCAT(PH.EntityName, ' (', T.HierarchyTypeName, ')'), ',') ParentChain
        	FROM JoinedOrgs JC
        	INNER JOIN Hierarchies CH ON CH.EntityId = JC.ClubDocId 
        	INNER JOIN Hierarchies PH ON CH.HierarchyId.IsDescendantOf(PH.HierarchyId) = 1 
            INNER JOIN HierarchyTypes T ON T.Id = PH.HierarchyTypeId 
        	WHERE CH.[HierarchyId] = JC.[HierarchyId] AND CH.EntityId <> -1 AND PH.EntityId <> -1
        	GROUP BY JC.ClubDocId
        )
        SELECT C.ClubDocId, C.ClubSyncGuid, C.[Image], C.ClubName, C.ClubId, C.LatLng, C.ClubMemberDocId, C.EmailAddress, C.ClubWebSite,
        SIGN(ISNULL(JC.ClubDocId, 0)) IsLowestTier,
        (SELECT [Value] FROM EntitySetting WHERE EntityId = C.ClubDocId AND ItemId = (Select ItemId From SystemSettings Where ItemKey = 'ORGANISATION.SOCIALMEDIA.LINKS')) SocialLinks,
        TS.TransferDocId,TS.TransferSyncGuid, TS.Tempnewclubname TransferClubName,
        P.ParentChain,
        S.MemberStatus, S.Roles MemberRoles, S.IsPrimary
        FROM JoinedOrgs C
        LEFT JOIN JoinedClubs JC ON JC.ClubDocId = C.ClubDocId
        LEFT JOIN TransferSummary TS ON TS.ClubDocId = C.ClubDocId
        INNER JOIN ParentChain P ON P.ClubDocId = C.ClubDocId
        LEFT JOIN CLUB_MEMBER_STATUS S ON S.ClubDocId = C.ClubDocId
        ORDER BY S.IsPrimary DESC

        """;

        return (await _readRepository.GetLazyRepository<MyOrganisation>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }

    private MyOrganisationDto MapToDto(MyOrganisation details)
    {
        return new MyOrganisationDto
        {
            ClubDocId = details.ClubDocId,
            ClubSyncGuid = details.ClubSyncGuid,
            ClubImagePath = string.IsNullOrWhiteSpace(details.Image) || details.Image == "Virtual" ? null : "/store/download?f=" + details.Image + "&t=repo&p=" + details.ClubDocId + "&p1=&p2=2",
            ClubName = details.ClubName,
            ClubId = details.ClubId,
            EmailAddress = details.EmailAddress,
            ClubWebSite = details.ClubWebSite,
            LatLng = details.LatLng,
            ClubMemberDocId = details.ClubMemberDocId,
            IsLowestTier = details.IsLowestTier,
            SocialLinks = details.SocialLinks,
            TransferSyncGuid = details.TransferSyncGuid,
            TransferMessage = details.TransferDocId is null ? null : "Pending Transfer to: " + details.TransferClubName,
            IsTransfer = details.TransferDocId is null ? false : true,
            Parents = details.ParentChain?.Split(",") ?? Array.Empty<string>(),
            MemberStatus = details.MemberStatus,
            Roles = details.MemberRoles?.Split(",") ?? Array.Empty<string>(),
            IsPrimary = details.IsPrimary
        };
    }
}