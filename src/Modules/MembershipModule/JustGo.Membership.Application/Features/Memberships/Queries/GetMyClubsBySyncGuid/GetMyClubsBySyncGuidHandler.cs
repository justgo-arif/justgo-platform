using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMyClubsBySyncGuid
{
    public class GetMyClubsBySyncGuidHandler : IRequestHandler<GetMyClubsBySyncGuidQuery, List<ClubInfoDto>>
    {
        private readonly LazyService<IReadRepository<ClubInfoDto>> _readRepository;
        private readonly LazyService<IReadRepository<LicenseDto>> _licenseRepository;

        public GetMyClubsBySyncGuidHandler(LazyService<IReadRepository<ClubInfoDto>> readRepository, LazyService<IReadRepository<LicenseDto>> licenseRepository)
        {
            _readRepository = readRepository;
            _licenseRepository = licenseRepository;
        }

        public async Task<List<ClubInfoDto>> Handle(GetMyClubsBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            var clubs = await GetClubsByUserSyncId(request.Id, cancellationToken);
            var licenses = await GetLicensesByMemberDocId(request.Id, cancellationToken);
            foreach (var club in clubs)
            {
                var clubLicenses = licenses.Where(l => l.ClubDocId == club.ClubDocId).ToList();
                club.Licenses = clubLicenses;
            }

            return clubs;
        }

        private async Task<List<ClubInfoDto>> GetClubsByUserSyncId(Guid userSyncId, CancellationToken cancellationToken)
        {

            var sql = $@"
                   DECLARE @UserSyncId UNIQUEIDENTIFIER = @UserSyncGuid;
                   DECLARE @UserId INT;
                   SELECT @UserId = Userid FROM [User] WHERE UserSyncId = @UserSyncId;

                   ;WITH UserClubs AS (
                     SELECT 
                         cd.DocId AS ClubDocId,
                          doc.SyncGuid AS ClubSyncGuid,
                         cd.ClubName,
                         cd.Location AS Image,
                         cmd.IsPrimary,
                 		cd.ClubId,
                         1 AS IsJoinedClub
                     FROM [User] u
                     JOIN Members_Links ml  ON ml.DocId = u.MemberDocId
                     JOIN ClubMembers_Default cmd ON cmd.DocId = ml.EntityId
                     JOIN ClubMembers_Links cml ON cml.DocId = cmd.DocId
                     JOIN Clubs_Default cd ON cd.DocId = cml.EntityId
                     INNER JOIN Document doc ON doc.DocId = cd.DocId
                     JOIN GoMembershipRegistry gmr ON gmr.EntityId = cd.DocId AND gmr.Status = 1
                     WHERE u.UserId = @UserId
                 ),
                 HierarchyWithParent AS (
                     SELECT 
                         CH.EntityId AS ChildEntityId,
                         PH.EntityId AS ParentEntityId,
                         PH.EntityName AS ParentEntityName,
                         HT.HierarchyTypeName,
                         ROW_NUMBER() OVER ( PARTITION BY CH.EntityId, PH.EntityId ORDER BY PH.HierarchyId) AS rn
                     FROM [dbo].[Hierarchies] CH
                     JOIN [dbo].[Hierarchies] PH ON CH.HierarchyId.IsDescendantOf(PH.HierarchyId) = 1
                     JOIN HierarchyTypes HT ON HT.Id = PH.HierarchyTypeId
                     WHERE CH.EntityId IN (SELECT ClubDocId FROM UserClubs)  AND PH.EntityId <> -1
                 ),
                 FilteredHierarchy AS (
                     SELECT DISTINCT ParentEntityId AS EntityId, ParentEntityName AS EntityName, HierarchyTypeName
                     FROM HierarchyWithParent WHERE rn = 1
                 ),
                 OrgLogo AS (
                     SELECT TOP 1 value AS Logo FROM SystemSettings WHERE ItemKey = 'ORGANISATION.LOGO'
                 )
                 SELECT 
                     uc.ClubDocId,
                     uc.ClubSyncGuid,
                     uc.ClubName,
                     uc.IsPrimary,
                 	uc.ClubId,
                     uc.IsJoinedClub,
                     uc.Image,
                     fh.HierarchyTypeName
                 FROM UserClubs uc
                 LEFT JOIN FilteredHierarchy fh 
                     ON fh.EntityId = uc.ClubDocId
                 
                 UNION ALL
                 
                 SELECT DISTINCT
                     0 AS ClubDocId,
                     '0' AS ClubSyncGuid,
                     fh.EntityName AS ClubName,
                     0 AS IsPrimary,
                 	'0' as ClubId,
                     1 AS IsJoinedClub,
                     o.Logo AS Image,
                     fh.HierarchyTypeName
                 FROM FilteredHierarchy fh
                 CROSS JOIN OrgLogo o
                 WHERE fh.EntityId = 0
                   AND NOT EXISTS (
                       SELECT 1 FROM UserClubs uc WHERE uc.ClubDocId = fh.EntityId
                   )
                 ORDER BY ClubDocId, ClubName;";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncGuid", userSyncId, dbType: DbType.Guid);
            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }

        private async Task<List<LicenseDto>> GetLicensesByMemberDocId(Guid userSyncId, CancellationToken cancellationToken)
        {
           var sql = @"
             DECLARE @UserSyncId UNIQUEIDENTIFIER = @UserSyncGuid;
             DECLARE @EntityId INT;
             SELECT @EntityId = MemberDocId FROM [User] WHERE UserSyncId = @UserSyncId;
             
             DECLARE @siteURl VARCHAR(100) = (SELECT [value] FROM SystemSettings WHERE ItemKey = 'SYSTEM.SITEADDRESS');
             
             IF (@siteURl = 'https://swimireland.justgo.com/')
             BEGIN
                 DECLARE @LinkRepositoryId INT = (SELECT DISTINCT RepositoryId FROM Document WHERE DocId IN (@EntityId));
             
                 DECLARE @LinkedEntity TABLE
                 (
                     DocId INT,
                     LicenseDocId INT UNIQUE CLUSTERED (LicenseDocId)
                 );
             
                 IF (@LinkRepositoryId = 1)
                     INSERT INTO @LinkedEntity
                     SELECT DISTINCT DocId, EntityId 
                     FROM Members_Links 
                     WHERE DocId IN (@EntityId) AND EntityParentId = 21;
             
                 IF (@LinkRepositoryId = 2)
                     INSERT INTO @LinkedEntity
                     SELECT DISTINCT DocId, EntityId 
                     FROM Clubs_Links 
                     WHERE DocId IN (@EntityId) AND EntityParentId = 21;
             
                 DECLARE @ExpiryDocId TABLE (LicenseDocId INT);
             
                 INSERT INTO @ExpiryDocId
                 SELECT mld.DocId
                 FROM MembersLicense_Default mld
                 INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mld.DocId
                 INNER JOIN Products_Default pd ON pd.DocId = mld.LicenceCode
                 INNER JOIN Document d ON d.DocId = pd.DocId
                 INNER JOIN License_Default ld ON ld.DocId = mld.LicenceNumber
                 INNER JOIN @LinkedEntity ml ON ml.LicenseDocId = mld.DocId
                 WHERE pri.CurrentStateId = 64
                 ORDER BY mld.EndDate DESC;
             
                 SELECT ml.DocId AS MemberDocId,
                        mld.DocId AS LicenseDocId,
                        mld.Reference AS LicenseId,
                        mld.Name,
                        mld.LicenceNumber,
                        mld.StartDate,
                        mld.EndDate AS ExpiryDate,
                        mld.LicenceCode AS ProductDocId,
                        mld.LicenceType,
                        ISNULL(pd.Ownerid, 0) AS ClubDocId,
                        pri.CurrentStateId,
                        st.Name AS [State],
                        pd.Color,
                        pd.Unitprice,
                        pd.Currency,
                        d.Location AS [Image],
                        ld.Benefits,
                        ld.Licencetype AS LicenseCategory,
                        ld.RenewalWindow,
                        ld.Reference,
                        ld.ExpiryDateEndingUnit,
                        ld.ExpiryDateEndingValue
                 FROM MembersLicense_Default mld
                 INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mld.DocId
                 INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                 INNER JOIN Products_Default pd ON pd.DocId = mld.LicenceCode
                 INNER JOIN Document d ON d.DocId = pd.DocId
                 INNER JOIN License_Default ld ON ld.DocId = mld.LicenceNumber
                 INNER JOIN @LinkedEntity ml ON ml.LicenseDocId = mld.DocId
                 WHERE pri.CurrentStateId IN (62, 63, 65, 66)
             
                 UNION
             
                 SELECT ml.DocId AS MemberDocId,
                        mld.DocId AS LicenseDocId,
                        mld.Reference AS LicenseId,
                        mld.Name,
                        mld.LicenceNumber,
                        mld.StartDate,
                        mld.EndDate AS ExpiryDate,
                        mld.LicenceCode AS ProductDocId,
                        mld.LicenceType,
                        ISNULL(pd.Ownerid, 0) AS ClubDocId,
                        pri.CurrentStateId,
                        st.Name AS [State],
                        pd.Color,
                        pd.Unitprice,
                        pd.Currency,
                        d.Location AS [Image],
                        ld.Benefits,
                        ld.Licencetype AS LicenseCategory,
                        ld.RenewalWindow,
                        ld.Reference,
                        ld.ExpiryDateEndingUnit,
                        ld.ExpiryDateEndingValue
                 FROM MembersLicense_Default mld
                 INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mld.DocId
                 INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                 INNER JOIN Products_Default pd ON pd.DocId = mld.LicenceCode
                 INNER JOIN Document d ON d.DocId = pd.DocId
                 INNER JOIN License_Default ld ON ld.DocId = mld.LicenceNumber
                 INNER JOIN @LinkedEntity ml ON ml.LicenseDocId = mld.DocId
                 WHERE pri.CurrentStateId = 64 
                   AND mld.DocId IN (SELECT LicenseDocId FROM @ExpiryDocId)
                 ORDER BY mld.EndDate DESC
                 OPTION (OPTIMIZE FOR UNKNOWN);
             END
             ELSE
             BEGIN
                 DECLARE @LinkRepositoryIdNew INT = (SELECT DISTINCT RepositoryId FROM Document WHERE DocId IN (@EntityId));
                 DECLARE @MemberGridEnabled VARCHAR(100) = (SELECT [value] FROM SystemSettings WHERE ItemKey = 'ORGANISATION.ENABLE_MEMBER_GRID_MODULE');
             
                 IF (@MemberGridEnabled = 'true' AND @LinkRepositoryIdNew = 1)
                 BEGIN
                     SELECT U.MemberDocId,
                            mld.MemberLicenseDocId AS LicenseDocId,
                            ML.Reference AS LicenseId,
                            ML.Name,
                            ML.LicenceNumber,
                            CAST(mld.StartDate AS DATETIME) AS StartDate,
                            CAST(mld.EndDate AS DATETIME) AS ExpiryDate,
                            ML.LicenceCode AS ProductDocId,
                            ML.LicenceType,
                            ISNULL(PD.Ownerid, 0) AS ClubDocId,
                            mld.StatusId AS CurrentStateId,
                            s.Name AS [State],
                            pd.Color,
                            pd.Unitprice,
                            pd.Currency,
                            d.Location AS [Image],
                            ld.Benefits,
                            ld.Licencetype AS LicenseCategory,
                            ld.RenewalWindow,
                            ld.Reference,
                            ld.ExpiryDateEndingUnit,
                            ld.ExpiryDateEndingValue
                     FROM UserMemberships mld
                     INNER JOIN [User] u ON u.UserId = mld.UserId
                     INNER JOIN MembersLicense_Default ML ON ML.DocId = mld.MemberLicenseDocId
                     INNER JOIN Products_Default PD ON PD.DocId = mld.ProductId
                     INNER JOIN State s ON s.StateId = mld.StatusId
                     INNER JOIN License_Links ll ON ll.EntityId = pd.DocId
                     INNER JOIN License_Default ld ON ld.DocId = ll.DocId
                     INNER JOIN Document d ON d.DocId = pd.DocId
                     WHERE u.MemberDocId IN (@EntityId)
                     ORDER BY CAST(mld.EndDate AS DATETIME) DESC;
                 END
                 ELSE IF (@MemberGridEnabled = 'true' AND @LinkRepositoryIdNew = 2)
                 BEGIN
                     SELECT mld.OrganisationId AS MemberDocId,
                            mld.MemberLicenseDocId AS LicenseDocId,
                            ML.Reference AS LicenseId,
                            ML.Name,
                            ML.LicenceNumber,
                            CAST(mld.StartDate AS DATETIME) AS StartDate,
                            CAST(mld.EndDate AS DATETIME) AS ExpiryDate,
                            ML.LicenceCode AS ProductDocId,
                            ML.LicenceType,
                            ISNULL(PD.Ownerid, 0) AS ClubDocId,
                            mld.StatusId AS CurrentStateId,
                            s.Name AS [State],
                            pd.Color,
                            pd.Unitprice,
                            pd.Currency,
                            d.Location AS [Image],
                            ld.Benefits,
                            ld.Licencetype AS LicenseCategory,
                            ld.RenewalWindow,
                            ld.Reference,
                            ld.ExpiryDateEndingUnit,
                            ld.ExpiryDateEndingValue
                     FROM OrganisationMemberships mld
                     INNER JOIN MembersLicense_Default ML ON ML.DocId = mld.MemberLicenseDocId
                     INNER JOIN Products_Default PD ON PD.DocId = mld.ProductId
                     INNER JOIN State s ON s.StateId = mld.StatusId
                     INNER JOIN License_Links ll ON ll.EntityId = pd.DocId
                     INNER JOIN License_Default ld ON ld.DocId = ll.DocId
                     INNER JOIN Document d ON d.DocId = pd.DocId
                     WHERE mld.OrganisationId IN (@EntityId)
                     ORDER BY CAST(mld.EndDate AS DATETIME) DESC;
                 END
                 ELSE
                 BEGIN
                     SELECT mld.EntityId AS MemberDocId,
                            mld.DocId AS LicenseDocId,
                            mld.Reference AS LicenseId,
                            mld.Name,
                            mld.LicenceNumber,
                            mld.StartDate,
                            mld.EndDate AS ExpiryDate,
                            mld.LicenceCode AS ProductDocId,
                            mld.LicenceType,
                            ISNULL(PD.Ownerid, 0) AS ClubDocId,
                            pri.CurrentStateId,
                            st.Name AS [State],
                            pd.Color,
                            pd.Unitprice,
                            pd.Currency,
                            d.Location AS [Image],
                            ld.Benefits,
                            ld.Licencetype AS LicenseCategory,
                            ld.RenewalWindow,
                            ld.Reference,
                            ld.ExpiryDateEndingUnit,
                            ld.ExpiryDateEndingValue
                     FROM MembersLicense_Default mld
                     INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mld.DocId
                     INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                     INNER JOIN Products_Default pd ON pd.DocId = mld.LicenceCode
                     INNER JOIN Document d ON d.DocId = pd.DocId
                     INNER JOIN License_Links ll ON ll.EntityId = pd.DocId
                     INNER JOIN License_Default ld ON ld.DocId = ll.DocId
                     INNER JOIN MembersLicense_Links mll ON mll.DocId = mld.DocId
                     WHERE ll.EntityParentId = 11 
                       AND mld.EntityId IN (@EntityId)
                     ORDER BY mld.EndDate DESC
                     OPTION (OPTIMIZE FOR UNKNOWN);
                 END
             END

           ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncGuid", userSyncId, dbType: DbType.Guid);
            var licenses = (await _licenseRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return licenses;
        }

    }
}