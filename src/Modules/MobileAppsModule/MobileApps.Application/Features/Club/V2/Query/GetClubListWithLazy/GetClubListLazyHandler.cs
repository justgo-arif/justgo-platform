using System.Collections.Generic;
using System.Data;
using System.Threading;
using AuthModule.Domain.Entities;
using Azure.Core;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.Club.Queries.GetClubList;
using MobileApps.Application.Features.Club.V2.Query.GetClubList;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities.V2;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using Pipelines.Sockets.Unofficial.Arenas;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MobileApps.Application.Features.Club.V2.Query.GetClubListWithLazy
{
    class GetClubListLazyHandler : IRequestHandler<GetClubListLazyQuery, List<Dictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
     
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetClubListLazyHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<List<Dictionary<string, object>>> Handle(GetClubListLazyQuery request, CancellationToken cancellationToken)
        {
            var results = new List<Dictionary<string, object>>();
            bool isAdmin = await IsUserInGroupsByUserId(request.UserId, "Admin,NGB Admin,NGB Finance");

           
            // Set paging and sorting parameters with sensible defaults
            int nextId = request.NextId > 0 ? request.NextId+1 : 0;
            int dataSize = request.DataSize > 0 ? (request.NextId + request.DataSize) : 100;
           
            string sortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "ASC" : request.SortOrder;
            if (isAdmin)
            {
                // Fetch main data "page"
                var data = await GetSwitchOptions(
                    "Club",
                    allowedType: "",
                    userId: request.UserId,
                    clubPlusOnly: request.IsClubPlusOnly,
                    clubName: request?.ClubName?.Trim(),
                     nextId,
                    dataSize,
                    sortOrder: sortOrder, cancellationToken
                );


                // Attach merchant info if Stripe mode enabled
                if (request.IsStripeMode)
                {
                    var clubMerchants = GetClubMerchantInfo();
                    var clubMerchantsDict = clubMerchants
                        .Select(cm => new
                        {
                            DocId = Convert.ToInt32(cm["DocId"]),
                            MerchantGuid = cm.ContainsKey("MerchantGuid") ? Convert.ToString(cm["MerchantGuid"]) : null
                        })
                        .ToDictionary(x => x.DocId, x => x.MerchantGuid);

                    foreach (var club in data)
                    {
                        if (club.TryGetValue("DocId", out var docIdObj) && int.TryParse(docIdObj.ToString(), out int docId))
                        {
                            if (clubMerchantsDict.TryGetValue(docId, out var guid))
                                club["MerchantGuid"] = guid;

                            club["EntityType"] = "Club";
                        }
                    }
                }

                // Optionally include Admin/NGB organizations at the top if user is admin and paging is at the start

                if (isAdmin && request.NextId <= 0)
                {
                    var organisation = GetOrganizationInfo()
                        .Select(r =>
                        {
                            var dict = new Dictionary<string, object>
                            {
                                ["DocId"] = r["DocId"],
                                ["SyncGuid"] = r["SyncGuid"],
                                ["Name"] = r["Name"],
                                ["Image"] = r.TryGetValue("Image", out var img) ? img : null,
                                ["EmailAddress"] = r.TryGetValue("Email", out var email) ? email : null,
                                ["RowNum"] = r.TryGetValue("RowNum", out var rowNum) ? rowNum : 1,
                                ["TotalCount"] = r.TryGetValue("TotalCount", out var totalCount) ? totalCount : 1
                                // Add more fields as necessary
                            };
                            return dict;
                        })
                        .Where(r => Convert.ToInt32(r["DocId"]) != -1)
                        .ToList();

                    if (organisation.Count > 0)
                        results.AddRange(organisation);
                }

                results.AddRange(data);
            }
            else
            {
                string sql = GetOthersClub();
                string whereFilterMember = @" WHERE u.Userid= @UserId AND (s.s <> 'Member' OR cd.IsPrimary = 1)";
                if(!string.IsNullOrEmpty(request.ClubName))
                {
                    whereFilterMember += " AND cd.ClubName LIKE @ClubName ";
                }
                // Set up parameters
                var sqlParams = new DynamicParameters();
                sqlParams.Add("@UserId", request.UserId);
                sqlParams.Add("@AllowedType", "");
                sqlParams.Add("@IsEventBookingArea","");
                sqlParams.Add("@IsEventManagerArea", "");
                sqlParams.Add("@IsClubPlusOnly", 0);
                sqlParams.Add("@ClubName", $"%{request.ClubName}%");
                sqlParams.Add("@NextId", nextId);
                sqlParams.Add("@DataSize", dataSize);
                // sqlParams.Add("@SortOrder", sortOrder); // Uncomment if sort order in SQL is implemented

                string pagedQuery = sql.Replace("@whereFilterMember", whereFilterMember);
                // Fetch and deserialize the data
                var reader = await _readRepository.Value.GetListAsync(
                    pagedQuery, sqlParams, null, "text"
                );

                if (reader != null)
                {
                    var json = JsonConvert.SerializeObject(reader);
                    var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                    results.AddRange(dataList);
                }
            }
              
            return results;
        }
        private async Task<List<Dictionary<string, object>>> GetSwitchOptions(string switchType,string allowedType,int userId,bool clubPlusOnly,string clubName,int nextId, int dataSize, string sortOrder, CancellationToken cancellationToken)
        {
            var datas = new List<Dictionary<string, object>>();
            int isEventBookingArea = 0;
            int isEventManagerArea = 0;
          
            // Sanitize club name and build filters
            string trimmedName = clubName?.Trim() ?? "";
            string filterWithWhere = string.IsNullOrEmpty(trimmedName)
                ? ""
                : " WHERE  tcd.ClubName LIKE @ClubName ";
            string filterOnly = string.IsNullOrEmpty(trimmedName)
                ? ""
                : " AND cd.ClubName LIKE @ClubName ";

            switch (switchType.ToLower())
            {
                case "club":
                    // Prepare SQL for paged club options
                    string pagedQuery = GetClubSql()
                        .Replace("@filterWithWhere", filterWithWhere)
                        .Replace("@filterOnly", filterOnly);

                    // Set up parameters
                    var sqlParams = new DynamicParameters();
                    sqlParams.Add("@UserId", userId);
                    sqlParams.Add("@AllowedType", allowedType);
                    sqlParams.Add("@IsEventBookingArea", isEventBookingArea);
                    sqlParams.Add("@IsEventManagerArea", isEventManagerArea);
                    sqlParams.Add("@IsClubPlusOnly", clubPlusOnly);
                    sqlParams.Add("@ClubName", $"%{clubName}%" );
                    sqlParams.Add("@NextId", nextId);
                    sqlParams.Add("@DataSize", dataSize);
                    // sqlParams.Add("@SortOrder", sortOrder); // Uncomment if sort order in SQL is implemented

                    // Fetch and deserialize the data
                    var reader = await _readRepository.Value.GetListAsync(
                        pagedQuery, sqlParams, null, "text"
                    );

                    if (reader != null)
                    {
                        var json = JsonConvert.SerializeObject(reader);
                        var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                        datas.AddRange(dataList);
                    }
                    break;

                default:
                    throw new NotImplementedException(
                        "Only Club case implemented for paging. Implement others as needed."
                    );
            }

            return datas;
        }

        private const string ISUSERINGROUP_ByUserId = @"
            select UserId from GroupMembers 
            where GroupId in (
                Select GroupId from [Group] where Name in (select s from dbo.SplitString(@Group,','))
            ) and UserId=@UserId";

        private async Task<bool> IsUserInGroupsByUserId(int userId, string Group)
        {
            var sqlParams = new DynamicParameters();
            sqlParams.Add("@UserId", userId);
            sqlParams.Add("@Group", Group);
            var reader = await _readRepository.Value.GetListAsync(ISUSERINGROUP_ByUserId, sqlParams, null, "text");
            return reader.Count() > 0;
        }

        private List<IDictionary<string, object>> GetOrganizationInfo()
        {
            string sql = @"
                select 
                case when (select value from SystemSettings
                where Itemkey = 'organisation.type') = 'NGB' Then 0 else -1 End DocId,
                case when (select value from SystemSettings
                where Itemkey = 'organisation.type') = 'NGB' 
                Then 
                    (Select top 1  d.SyncGuid SyncGuid 
                        from merchantprofile_default mpd 
                        Inner join Document d on d.docid=mpd.docid
                        WHERE mpd.docid NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
                        AND mpd.Name != 'JustGo' and mpd.Merchanttype = 'NGB')
                else (Select top 1  d.SyncGuid  
                        from merchantprofile_default mpd 
                        Inner join Document d on d.docid=mpd.docid
                        WHERE mpd.docid NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
                        AND mpd.Name != 'JustGo') 
                    End SyncGuid,
                (select value from SystemSettings where Itemkey = 'organisation.name') [Name],
                (select value from SystemSettings where Itemkey = 'organisation.LOGO') [Image],
                (select value from SystemSettings where Itemkey = 'ORGANISATION.CONTACT_EMAIL_ADDRESS') Email";                                                     

            var rows = new List<IDictionary<string, object>>();
            var reader = _readRepository.Value.GetList(sql, null, null, "text");
            if (reader != null && reader.Count() > 0)
            {
                var json = JsonConvert.SerializeObject(reader);
                var dataList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(json);
                foreach (var item in dataList)
                {
                    rows.Add(item);
                }
            }
            return rows;
        }

        private List<IDictionary<string, object>> GetClubMerchantInfo()
        {
            string sql = @"
                Select cd.DocId, d.syncguid MerchantGuid 
                FROM Clubs_default cd 
                inner join merchantprofile_links mpl on cd.DocId = mpl.entityid and mpl.entityparentid=2
                inner join merchantprofile_default mpd on mpd.docid=mpl.docid
                inner join Document d on d.docid=mpd.docid";

            var rows = new List<IDictionary<string, object>>();
            var reader = _readRepository.Value.GetList(sql, null, null, "text");
            if (reader != null && reader.Count() > 0)
            {
                var json = JsonConvert.SerializeObject(reader);
                var dataList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(json);
                foreach (var item in dataList)
                {
                    rows.Add(item);
                }
            }
            return rows;
        }

        private static string GetClubSql()
        {
            return @"-- Variable Declarations
            DECLARE @RegionValue VARCHAR(200) = '';
            DECLARE @SubRegionValue VARCHAR(200) = '';

            -- Get region/sub-region values from settings
            IF EXISTS (
                SELECT value FROM SystemSettings WHERE ItemKey = 'ORGANISATION.REGIONAL_ENTITY_IDENTITY'
            )
                SET @RegionValue = (SELECT value FROM SystemSettings WHERE ItemKey = 'ORGANISATION.REGIONAL_ENTITY_IDENTITY');
            ELSE
                SET @RegionValue = 'Region';

            IF EXISTS (
                SELECT value FROM SystemSettings WHERE itemkey = 'ORGANISATION.SUB_REGIONAL_ENTITY_IDENTITY'
            )
                SET @SubRegionValue = (SELECT value FROM SystemSettings WHERE itemkey = 'ORGANISATION.SUB_REGIONAL_ENTITY_IDENTITY');
            ELSE
                SET @SubRegionValue = '';

            -- Get Region and SubRegion field names dynamically
            DECLARE @REGIONField NVARCHAR(MAX) = (SELECT Value FROM SystemSettings WHERE ItemKey = 'CLUB.REGION_IDENTIFIER_FIELD');
            IF (LEN(@REGIONField) > 0 AND (SELECT TOP 1 s FROM dbo.splitstring(@REGIONField, ':') WHERE zeroBasedOccurance = 0) = 'FM')
                SELECT @REGIONField = s FROM dbo.splitstring(@REGIONField, ':') WHERE zeroBasedOccurance = 1;
            ELSE
                SET @REGIONField = '';

            DECLARE @SubREGIONField NVARCHAR(MAX) = (SELECT Value FROM SystemSettings WHERE ItemKey = 'CLUB.SUB_REGION_IDENTIFIER_FIELD');
            IF (LEN(@SubREGIONField) > 0 AND (SELECT TOP 1 s FROM dbo.splitstring(@SubREGIONField, ':') WHERE zeroBasedOccurance = 0) = 'FM')
                SELECT @SubREGIONField = s FROM dbo.splitstring(@SubREGIONField, ':') WHERE zeroBasedOccurance = 1;
            ELSE
                SET @SubREGIONField = '';

            -- Get setting for regional admin club view
            DECLARE @REGIONAL_ADMIN_VIEW_ALL_CLUBS BIT = (
                SELECT CASE WHEN [Value] = 'true' THEN 1 ELSE 0 END FROM SystemSettings WHERE ItemKey = 'ORGANISATION.REGIONAL_ADMIN_VIEW_ALL_CLUBS'
            );

            -- Create PermissionGrouping table if not exists
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = 'PermissionGrouping')
            BEGIN
                CREATE TABLE [dbo].[PermissionGrouping](
                    [Id] [int] IDENTITY(1,1) NOT NULL,
                    [PermissionGroupName] [varchar](500) NOT NULL,
                    [PermissionGroupId] [int] NOT NULL,
                    CONSTRAINT [PK_PermissionGrouping] PRIMARY KEY CLUSTERED ([Id] ASC)
                );
                INSERT INTO PermissionGrouping(PermissionGroupName, PermissionGroupId) VALUES ('NGB', 25);
            END

            -- ADMIN SHORTCUT
            IF EXISTS(
                SELECT GroupId FROM GroupMembers 
                WHERE UserId = @UserId 
                    AND (GroupId = 1 OR GroupId IN (
                        SELECT PermissionGroupId FROM PermissionGrouping WHERE PermissionGroupName = 'NGB' AND PermissionGroupId = 25
                    ))
            )
            BEGIN
                ;WITH ClubPaged AS (
                    SELECT doc.SyncGuid, cd.DocId, cd.ClubName AS [Name], cd.ClubId AS Reference, cd.Location AS [Image], 
                        cd.ClubaddressLine1 AS Address1, cd.ClubaddressLine2 AS Address2, cd.ClubaddressLine3 AS Address3, 
                        cd.Clubtown AS Town, cd.Clubpostcode AS PostCode, cd.ClubPhoneNumber AS Phone, cd.ClubemailAddress AS EmailAddress,
                        cd.Region AS County, cd.ClubCountry AS Country, cd.ClubWebsite AS Website, cd.ClubType AS EntityType,
                        ROW_NUMBER() OVER(ORDER BY cd.ClubName asc) AS RowNum,
                        COUNT(*) OVER() AS TotalCount
                    FROM Clubs_Default cd
                    INNER JOIN Document doc ON doc.DocId = cd.DocId
                    WHERE (@AllowedType IS NULL OR @AllowedType = '' OR cd.ClubType IN (SELECT s FROM dbo.SplitString(@AllowedType, ',')))
                        AND (@IsClubPlusOnly = 0 OR cd.DocId IN (Select distinct EntityId from GoMembershipRegistry where status = 1))
                        @filterOnly 
                )
                SELECT *
                FROM ClubPaged
                WHERE RowNum BETWEEN @NextId AND @DataSize
                ORDER BY RowNum OPTION(OPTIMIZE FOR UNKNOWN);
                RETURN;
            END

            -- DYNAMIC CTE ALL CLUBS
            DECLARE @LookUpId INT = (SELECT LookupId FROM LookUp WHERE Name = 'Club Role');

            DECLARE @RoleField VARCHAR(20) = (SELECT 'Field_' + CAST(LookUpFieldId AS VARCHAR(10)) FROM LookUpFields WHERE LookupId = @LookUpId AND Name = 'Name');
            DECLARE @AdminField VARCHAR(20) = (SELECT 'Field_' + CAST(LookUpFieldId AS VARCHAR(10)) FROM LookUpFields WHERE LookupId = @LookUpId AND Name = 'IsAdmin');
            DECLARE @EventAcessField VARCHAR(20) = (SELECT 'Field_' + CAST(LookUpFieldId AS VARCHAR(10)) FROM LookUpFields WHERE LookupId = @LookUpId AND Name = 'Event Access');
            DECLARE @EmailAcessField VARCHAR(20) = (SELECT 'Field_' + CAST(LookUpFieldId AS VARCHAR(10)) FROM LookUpFields WHERE LookupId = @LookUpId AND Name = 'Email Access');
            DECLARE @HasBookingAccessField VARCHAR(20) = (SELECT 'Field_' + CAST(LookUpFieldId AS VARCHAR(10)) FROM LookUpFields WHERE LookupId = @LookUpId AND Name = 'HasBookingAccess');

            -- Build Dynamic SQL for field/table reference
            DECLARE @sql NVARCHAR(MAX) = N'
            ;WITH tempclub_default AS (
                -- 1. Direct club/member role access
                SELECT DISTINCT c.DocId, c.ClubName, c.ClubId, c.Location, c.ClubaddressLine1, c.ClubaddressLine2, c.ClubaddressLine3,
                    c.Clubtown, c.Clubpostcode, c.ClubPhoneNumber, c.ClubemailAddress, c.Region, c.ClubCountry, c.ClubWebsite, c.ClubType
                FROM Clubs_Default c
                WHERE c.DocId IN (
                    SELECT cl.DocId FROM Clubs_Links cl
                    WHERE cl.EntityParentId = 3 AND (
                        cl.EntityId IN (
                            SELECT cd.DocId FROM ClubMembers_Default cd
                            CROSS APPLY dbo.SplitString(cd.MyRoles, '''') AS [Role]
                            WHERE cd.DocId IN (
                                SELECT cml.DocId FROM ClubMembers_Links cml
                                WHERE cml.EntityParentId = 1 AND cml.EntityId IN (
                                    SELECT md.DocId FROM Members_Default md WHERE md.DocId IN (
                                        SELECT el.LinkId FROM EntityLink el WHERE el.SourceId = @UserId AND el.LinkParentId = 1
                                    )
                                )
                            )
                            AND (
                                [Role].s IN (
                                    SELECT [' + @RoleField + '] FROM lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' WHERE [' + @RoleField + '] = RoleName AND [' + @AdminField + '] = ''Yes''
                                )
                                OR (
                                    [Role].s IN (
                                        SELECT [' + @RoleField + '] FROM lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' WHERE [' + @RoleField + '] = RoleName AND [' + @EventAcessField + '] = ''Yes''
                                    ) AND 1 = @isEventManagerArea
                                )
                                OR (
                                    [Role].s IN (
                                        SELECT [' + @RoleField + '] FROM lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' WHERE [' + @RoleField + '] = RoleName AND [' + @EmailAcessField + '] = ''Yes''
                                    )
                                )
                                OR (
                                    [Role].s IN (
                                        SELECT [' + @RoleField + '] FROM lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' WHERE [' + @RoleField + '] = RoleName AND [' + @HasBookingAccessField + '] = ''Yes''
                                    ) AND 1 = @isEventBookingArea
                                )
                            )
                        )
                    )
                )
                AND (@AllowedType IS NULL OR @AllowedType = '''' OR c.ClubType IN (SELECT s FROM dbo.SplitString(@AllowedType, '','')))
                AND (@IsClubPlusOnly = 0 OR c.DocId IN (Select distinct EntityId from GoMembershipRegistry where status = 1))

                UNION

                -- 2. Regional admin club access
                SELECT DISTINCT c.DocId, c.ClubName, c.ClubId, c.Location, c.ClubaddressLine1, c.ClubaddressLine2, c.ClubaddressLine3,
                    c.Clubtown, c.Clubpostcode, c.ClubPhoneNumber, c.ClubemailAddress, c.Region, c.ClubCountry, c.ClubWebsite, c.ClubType
                FROM Clubs_Default c
                WHERE (
                    (LEN(@REGIONField) > 0 AND c.DocId IN (
                        SELECT DISTINCT ent.DocId FROM ExNgbClub_LargeText ent
                        WHERE ent.fieldid = @REGIONField AND ent.value IN (
                            SELECT DISTINCT cd.ClubName FROM Members_Default md
                            INNER JOIN Members_links ml ON ml.docid = md.docid
                            INNER JOIN clubmembers_default cmd ON cmd.docid = ml.entityid
                            INNER JOIN clubmembers_links cml ON cml.docid = cmd.docid
                            INNER JOIN Clubs_Default cd ON cd.Docid = cml.entityId
                            INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
                            INNER JOIN [user] u ON u.userid = el.SourceId
                            CROSS APPLY dbo.SplitString(cmd.MyRoles, '''') AS [Role]
                            INNER JOIN lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' cl ON cl.[' + @RoleField + '] = [Role].s
                            WHERE cl.[' + @AdminField + '] = ''Yes'' AND u.userid = @UserId AND cd.ClubType = @RegionValue
                        )
                    ))
                    OR
                    (c.DocId IN (
                        SELECT cd2.DocId FROM Clubs_Default cd2 WHERE region IN (
                            SELECT DISTINCT cd.ClubName FROM Members_Default md
                            INNER JOIN Members_links ml ON ml.docid = md.docid
                            INNER JOIN clubmembers_default cmd ON cmd.docid = ml.entityid
                            INNER JOIN clubmembers_links cml ON cml.docid = cmd.docid
                            INNER JOIN Clubs_Default cd ON cd.Docid = cml.entityId
                            INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
                            INNER JOIN [user] u ON u.userid = el.SourceId
                            CROSS APPLY dbo.SplitString(cmd.MyRoles, '''') AS [Role]
                            INNER JOIN lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' cl ON cl.[' + @RoleField + '] = [Role].s
                            WHERE cl.[' + @AdminField + '] = ''Yes'' AND u.userid = @UserId AND cd.ClubType = @RegionValue
                        )
                    ))
                ) AND @REGIONAL_ADMIN_VIEW_ALL_CLUBS = 1
                AND (@AllowedType IS NULL OR @AllowedType = '''' OR c.ClubType IN (SELECT s FROM dbo.SplitString(@AllowedType, '','')))
                AND (@IsClubPlusOnly = 0 OR c.DocId IN (Select distinct EntityId from GoMembershipRegistry where status = 1))

                UNION

                -- 3. Subregion admin club access
                SELECT DISTINCT c.DocId, c.ClubName, c.ClubId, c.Location, c.ClubaddressLine1, c.ClubaddressLine2, c.ClubaddressLine3,
                    c.Clubtown, c.Clubpostcode, c.ClubPhoneNumber, c.ClubemailAddress, c.Region, c.ClubCountry, c.ClubWebsite, c.ClubType
                FROM Clubs_Default c
                WHERE (
                    (LEN(@SubREGIONField) > 0 AND c.DocId IN (
                        SELECT DISTINCT ent.DocId FROM ExNgbClub_LargeText ent
                        WHERE ent.fieldid = @SubREGIONField AND ent.value IN (
                            SELECT DISTINCT cd.ClubName FROM Members_Default md
                            INNER JOIN Members_links ml ON ml.docid = md.docid
                            INNER JOIN clubmembers_default cmd ON cmd.docid = ml.entityid
                            INNER JOIN clubmembers_links cml ON cml.docid = cmd.docid
                            INNER JOIN Clubs_Default cd ON cd.Docid = cml.entityId
                            INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
                            INNER JOIN [user] u ON u.userid = el.SourceId
                            CROSS APPLY dbo.SplitString(cmd.MyRoles, '''') AS [Role]
                            INNER JOIN lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' cl ON cl.[' + @RoleField + '] = [Role].s
                            WHERE cl.[' + @AdminField + '] = ''Yes'' AND u.userid = @UserId AND cd.ClubType = @SubRegionValue
                        )
                    ))
                    OR 1 = 1
                ) AND @REGIONAL_ADMIN_VIEW_ALL_CLUBS = 1
                AND (@AllowedType IS NULL OR @AllowedType = '''' OR c.ClubType IN (SELECT s FROM dbo.SplitString(@AllowedType, '','')))
                AND (@IsClubPlusOnly = 0 OR c.DocId IN (Select distinct EntityId from GoMembershipRegistry where status = 1))

                UNION

                -- 4. Region booking access
                SELECT DISTINCT c.DocId, c.ClubName, c.ClubId, c.Location, c.ClubaddressLine1, c.ClubaddressLine2, c.ClubaddressLine3,
                    c.Clubtown, c.Clubpostcode, c.ClubPhoneNumber, c.ClubemailAddress, c.Region, c.ClubCountry, c.ClubWebsite, c.ClubType
                FROM Clubs_Default c
                WHERE (
                    (LEN(@REGIONField) > 0 AND c.DocId IN (
                        SELECT DISTINCT ent.DocId FROM ExNgbClub_LargeText ent
                        WHERE ent.fieldid = @REGIONField AND ent.value IN (
                            SELECT DISTINCT cd.ClubName FROM Members_Default md
                            INNER JOIN Members_links ml ON ml.docid = md.docid
                            INNER JOIN clubmembers_default cmd ON cmd.docid = ml.entityid
                            INNER JOIN clubmembers_links cml ON cml.docid = cmd.docid
                            INNER JOIN Clubs_Default cd ON cd.Docid = cml.entityId
                            INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
                            INNER JOIN [user] u ON u.userid = el.SourceId
                            CROSS APPLY dbo.SplitString(cmd.MyRoles, '''') AS [Role]
                            INNER JOIN lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' cl ON cl.[' + @RoleField + '] = [Role].s
                            WHERE cl.[' + @HasBookingAccessField + '] = ''Yes'' AND u.userid = @UserId AND cd.ClubType = @RegionValue
                        )
                    ) AND @IsEventBookingArea = 1)
                    OR (
                        c.DocId IN (
                            SELECT cd2.DocId FROM Clubs_Default cd2 WHERE region IN (
                                SELECT DISTINCT cd.ClubName FROM Members_Default md
                                INNER JOIN Members_links ml ON ml.docid = md.docid
                                INNER JOIN clubmembers_default cmd ON cmd.docid = ml.entityid
                                INNER JOIN clubmembers_links cml ON cml.docid = cmd.docid
                                INNER JOIN Clubs_Default cd ON cd.Docid = cml.entityId
                                INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
                                INNER JOIN [user] u ON u.userid = el.SourceId
                                CROSS APPLY dbo.SplitString(cmd.MyRoles, '''') AS [Role]
                                INNER JOIN lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' cl ON cl.[' + @RoleField + '] = [Role].s
                                WHERE cl.[' + @HasBookingAccessField + '] = ''Yes'' AND u.userid = @UserId AND cd.ClubType = @RegionValue
                            )
                        ) AND @IsEventBookingArea = 1
                    )
                ) AND @REGIONAL_ADMIN_VIEW_ALL_CLUBS = 1
                AND (@AllowedType IS NULL OR @AllowedType = '''' OR c.ClubType IN (SELECT s FROM dbo.SplitString(@AllowedType, '','')))
                AND (@IsClubPlusOnly = 0 OR c.DocId IN (Select distinct EntityId from GoMembershipRegistry where status = 1))

                UNION

                -- 5. Subregion booking access
                SELECT DISTINCT c.DocId, c.ClubName, c.ClubId, c.Location, c.ClubaddressLine1, c.ClubaddressLine2, c.ClubaddressLine3,
                    c.Clubtown, c.Clubpostcode, c.ClubPhoneNumber, c.ClubemailAddress, c.Region, c.ClubCountry, c.ClubWebsite, c.ClubType
                FROM Clubs_Default c
                WHERE (
                    (LEN(@SubREGIONField) > 0 AND c.DocId IN (
                        SELECT DISTINCT ent.DocId FROM ExNgbClub_LargeText ent
                        WHERE ent.fieldid = @SubREGIONField AND ent.value IN (
                            SELECT DISTINCT cd.ClubName FROM Members_Default md
                            INNER JOIN Members_links ml ON ml.docid = md.docid
                            INNER JOIN clubmembers_default cmd ON cmd.docid = ml.entityid
                            INNER JOIN clubmembers_links cml ON cml.docid = cmd.docid
                            INNER JOIN Clubs_Default cd ON cd.Docid = cml.entityId
                            INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
                            INNER JOIN [user] u ON u.userid = el.SourceId
                            CROSS APPLY dbo.SplitString(cmd.MyRoles, '''') AS [Role]
                            INNER JOIN lookup_' + CAST(@LookUpId AS VARCHAR(10)) + ' cl ON cl.[' + @RoleField + '] = [Role].s
                            WHERE cl.[' + @HasBookingAccessField + '] = ''Yes'' AND u.userid = @UserId AND cd.ClubType = @SubRegionValue
                        )
                    ) AND @IsEventBookingArea = 1)
                    OR (@IsEventBookingArea = 1)
                ) AND @REGIONAL_ADMIN_VIEW_ALL_CLUBS = 1
                AND (@AllowedType IS NULL OR @AllowedType = '''' OR c.ClubType IN (SELECT s FROM dbo.SplitString(@AllowedType, '','')))
                AND (@IsClubPlusOnly = 0 OR c.DocId IN (Select distinct EntityId from GoMembershipRegistry where status = 1))
            )
            , ClubPaged AS (
                SELECT DISTINCT
                    doc.SyncGuid, 
                    tcd.DocId, tcd.ClubName AS [Name], tcd.ClubId AS Reference, tcd.Location AS [Image],
                    tcd.ClubaddressLine1 AS Address1, tcd.ClubaddressLine2 AS Address2, tcd.ClubaddressLine3 AS Address3,
                    tcd.Clubtown AS Town, tcd.Clubpostcode AS PostCode, tcd.ClubPhoneNumber AS Phone, tcd.ClubemailAddress AS EmailAddress,
                    tcd.Region AS County, tcd.ClubCountry AS Country, tcd.ClubWebsite AS Website, tcd.ClubType AS EntityType,
                    ROW_NUMBER() OVER(ORDER BY tcd.ClubName asc) AS RowNum,
                    COUNT(*) OVER () AS TotalCount
                FROM tempclub_default tcd
                INNER JOIN Document doc ON doc.DocId = tcd.DocId
                @filterWithWhere
            )
            SELECT *
            FROM ClubPaged
            WHERE RowNum BETWEEN @NextId AND @DataSize
            ORDER BY RowNum;
            '";

        }

        private static string GetOthersClub() {
            return @";with ClubPaged as (
                    SELECT DISTINCT c.DocId, c.ClubName as Name, c.ClubId, c.Location as Image, c.ClubaddressLine1, c.ClubaddressLine2, c.ClubaddressLine3,
                    c.Clubtown, c.Clubpostcode, c.ClubPhoneNumber, c.ClubemailAddress, c.Region, c.ClubCountry, c.ClubWebsite, c.ClubType,
                     d.SyncGuid,
                    ROW_NUMBER() OVER (ORDER BY c.ClubName) AS RowNum,
                    COUNT(*) OVER () AS TotalCount
                    FROM Clubs_Default c
                    inner join Clubs_Links cl on c.DocId=cl.DocId
                    inner join ClubMembers_Default cd on cl.EntityId=cd.DocId
                    inner join [User] u on u.MemberId=cd.MemberId
                    Inner join Document d on d.DocId=c.DocId
                    CROSS APPLY dbo.SplitString(cd.MyRoles, ',') as  s
                    @whereFilterMember
                   
                )
                SELECT *
                FROM ClubPaged
                WHERE RowNum BETWEEN @NextId AND @DataSize
                ORDER BY RowNum
                OPTION (OPTIMIZE FOR UNKNOWN);";
        
        }
    }
}
