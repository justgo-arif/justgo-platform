using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetClubDetails;

public class GetClubDetailsHandler : IRequestHandler<GetClubDetailsQuery, ClubDetailsDto?>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetClubDetailsHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<ClubDetailsDto?> Handle(GetClubDetailsQuery request, CancellationToken cancellationToken)
    {
        return await GetClubDetailsAsync(request, cancellationToken) ?? throw new KeyNotFoundException("Club not found");
    }

    private async Task<ClubDetailsDto?> GetClubDetailsAsync(GetClubDetailsQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("ClubSyncId", request.ClubGuid);
        queryParameters.Add("UserSyncId", request.UserGuid);

        var sql = """
            WITH CLUB AS (
                SELECT 
                CD.DocId ClubDocId,
                D.SyncGuid,
                CD.ClubName,
                CD.ClubID,
                CD.ClubPhoneNumber PhoneNumber,
                CD.ClubemailAddress EmailAddress,
                CD.ClubaddressLine1 Address1,
                CD.ClubaddressLine2 Address2,
                CD.ClubaddressLine3 Address3,
                CD.Clubtown Town,
                CD.Clubpostcode Postcode,
                CD.Region County,
                CD.ClubCountry Country,
                CD.[Location] ClubImage,
                CD.ClubWebSite,
                TRY_CAST(SUBSTRING(CD.Latlng, 1, CHARINDEX(',', CD.Latlng) - 1) AS FLOAT) AS Lat,
                TRY_CAST(SUBSTRING(CD.Latlng, CHARINDEX(',', CD.Latlng) + 1, LEN(CD.Latlng)) AS FLOAT) AS Lng,
                dbo.CalculateDistance(0, 0, (ISNULL(SUBSTRING(CD.[Latlng], 0, CHARINDEX(',', CD.[Latlng])), 0)),
                (ISNULL(SUBSTRING(CD.[Latlng], CHARINDEX(',', CD.[Latlng]) + 1, LEN(CD.[Latlng])), 0)), 1) Distance,
                (SELECT [Value] FROM EntitySetting WHERE EntityId = CD.DocId AND ItemId = (Select ItemId From SystemSettings Where ItemKey = 'ORGANISATION.SOCIALMEDIA.LINKS')) SocialLinks
                FROM Clubs_Default CD
                INNER JOIN [Document] D ON D.DocId = CD.DocId 
                WHERE D.SyncGuid = @ClubSyncId
            ),
            JOIND_CLUBS AS (
                SELECT CD.DocId, CMD.RegisterDate JoinedDate, U.MemberDocId
                FROM [User] U
                INNER JOIN Members_links ml on ml.docid = U.MemberDocId AND Entityparentid = 3
                INNER JOIN clubmembers_default cmd on cmd.docid = ml.entityid
                INNER JOIN clubmembers_links cml on cml.docid = cmd.docid
                INNER JOIN Clubs_Default cd on cd.Docid = cml.entityId
                INNER JOIN CLUB ON CLUB.ClubDocId = CD.DocId
                WHERE U.UserSyncId = @UserSyncId
            ),
            TransferSummary AS (
            	SELECT DISTINCT JC.DocId ClubDocId, td.DocId TransferDocId, td.Tempnewclubname, s.[Name] TransferStatus 
            	FROM JOIND_CLUBS JC 
            	INNER JOIN Transfers_Default td on td.FromClubId=JC.DocId
            	INNER JOIN Transfers_Links tl on tl.Docid=td.DocId AND tl.EntityId=JC.MemberDocId
            	INNER JOIN ProcessInfo p ON p.PrimaryDocId = td.DocId  --Need process Id
            	INNER JOIN [State] s ON s.StateId = p.CurrentStateId
            	WHERE s.Name IN ('Awaiting Approval NGB', 'Awaiting Approval Current Club', 'Awaiting Approval New Club')
            ),
            CLUB_MEMBER_INFO AS (
                SELECT CMR.ClubDocId, S.[Name] MemberStatus, CMR.RoleName, CMR.IsPrimary
                FROM ClubMemberRoles CMR
                INNER JOIN ProcessInfo p on p.PrimaryDocId=CMR.ClubMemberDocId
                INNER JOIN [State] S on s.StateId=p.CurrentStateId
                INNER JOIN CLUB ON CLUB.ClubDocId = CMR.ClubDocId
                INNER JOIN [User] U ON U.UserId = CMR.UserId AND U.UserSyncId = @UserSyncId
            ),
            CLUB_MEMBER_STATUS AS (
                SELECT CM.ClubDocId, 
                (SELECT STRING_AGG(MemberStatus, ',') FROM (SELECT DISTINCT MemberStatus FROM CLUB_MEMBER_INFO CMI WHERE CMI.ClubDocId = CM.ClubDocId) AS DS) AS MemberStatus,
                (SELECT STRING_AGG(RoleName, ',') FROM (SELECT DISTINCT RoleName FROM CLUB_MEMBER_INFO CMI WHERE CMI.ClubDocId = CM.ClubDocId) AS DR) AS Roles,
                MAX(CAST(CM.IsPrimary AS INT)) IsPrimary
                FROM CLUB_MEMBER_INFO CM
                GROUP BY CM.ClubDocId
            )
            SELECT 
            C.ClubDocId, C.SyncGuid, C.ClubName, C.ClubID, C.PhoneNumber, C.EmailAddress, C.Address1, C.Address2, C.Address3,
            C.Town, C.Postcode, C.County, C.Country, C.ClubImage, C.Lat, C.Lng, C.Distance, C.SocialLinks, C.ClubWebSite,
            SIGN(ISNULL(JC.DocId, 0)) IsJoined, JC.JoinedDate,
            S.MemberStatus, S.Roles MemberRoles, S.IsPrimary, TS.TransferDocId, TS.Tempnewclubname TransferClubName
            FROM CLUB C
            LEFT JOIN CLUB_MEMBER_STATUS S ON S.ClubDocId = C.ClubDocId
            LEFT JOIN JOIND_CLUBS JC ON JC.DocId = C.ClubDocId
            LEFT JOIN TransferSummary TS ON TS.ClubDocId = C.ClubDocId
            """;

        return await _readRepository.GetLazyRepository<ClubDetailsDto>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
    }
}


