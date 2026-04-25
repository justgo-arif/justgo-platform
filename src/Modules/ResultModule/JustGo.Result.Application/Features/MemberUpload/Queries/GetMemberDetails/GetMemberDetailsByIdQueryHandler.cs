using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberDetails
{
    public class GetMemberDetailsByIdQueryHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<GetMemberDetailsByIdQuery, MemberDetailsDto?>
    {
        private const string SqlQuery = """
       DECLARE @BaseUrl NVARCHAR(500);
    DECLARE @MemberIdItemKey NVARCHAR(100) = '';
    DECLARE @MemberFirstNameKey NVARCHAR(100) = '';
    DECLARE @MemberLastNameKey NVARCHAR(100) = '';
    DECLARE @AssetIdItemKey NVARCHAR(100) = '';
    DECLARE @AssetNameItemKey NVARCHAR(100) = '';
    
    SELECT 
        @BaseUrl = ss.[Value],
        @MemberIdItemKey = 'Member Id',
        @MemberFirstNameKey = 'First Name',
        @MemberLastNameKey = 'Last Name',
        @AssetIdItemKey = 'Horse ID',
        @AssetNameItemKey = 'Horse Name'
    FROM SystemSettings ss
    WHERE ss.ItemKey = 'SYSTEM.SITEADDRESS';
    
    WITH CompleteUserData AS (
        SELECT 
            umd.UploadedMemberDataId,
            umd.MemberData,
            um.UserId,
            u.ProfilePicURL,
            u.Mobile,
            u.EmailAddress,
            CASE 
                WHEN @AssetIdItemKey != '' AND EXISTS (
                    SELECT 1 
                    FROM OPENJSON(umd.MemberData) j
                    INNER JOIN AssetRegisters ar ON ar.AssetReference = CAST(j.[value] AS NVARCHAR(100))
                    WHERE LOWER(CAST(j.[key] AS NVARCHAR(100))) = LOWER(@AssetIdItemKey) -- Fixed: Added LOWER()
                ) THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END AS IsHorseExists
        FROM ResultUploadedMemberData umd
        INNER JOIN ResultUploadedMember um ON um.UploadedMemberId = umd.UploadedMemberId
        LEFT JOIN [User] u ON um.UserId = u.UserId
        WHERE umd.UploadedMemberDataId = @Id 
          AND um.IsDeleted = 0
    ),
    MembershipData AS (
        SELECT 
            cud.UserId,
            COUNT(DISTINCT pd.DocId) AS MembershipTypeCount,
            MAX(pd.Name) AS MembershipName
        FROM CompleteUserData cud
        LEFT JOIN UserMemberships um_membership ON um_membership.UserId = cud.UserId AND cud.UserId > 0
        LEFT JOIN processinfo pr ON pr.primarydocid = um_membership.MemberLicenseDocId AND pr.CurrentStateId = 62
        LEFT JOIN Products_Default pd ON pd.DocId = um_membership.ProductId
        GROUP BY cud.UserId
    ),
    JsonKeys AS (
        SELECT 
            cud.UploadedMemberDataId,
            cud.IsHorseExists,
            cud.UserId,
            cud.ProfilePicURL,
            cud.Mobile,
            cud.EmailAddress,
            md.MembershipName,
            md.MembershipTypeCount,
            CAST(j.[key] AS NVARCHAR(100)) AS JsonKey,
            CAST(j.[value] AS NVARCHAR(MAX)) AS JsonValue,
            CASE 
                WHEN LOWER(CAST(j.[key] AS NVARCHAR(100))) = LOWER(@MemberIdItemKey) THEN 1
                WHEN LOWER(CAST(j.[key] AS NVARCHAR(100))) = LOWER(@MemberFirstNameKey) THEN 2
                WHEN LOWER(CAST(j.[key] AS NVARCHAR(100))) = LOWER(@MemberLastNameKey) THEN 3
                WHEN LOWER(CAST(j.[key] AS NVARCHAR(100))) = LOWER(@AssetIdItemKey) THEN 4
                WHEN LOWER(CAST(j.[key] AS NVARCHAR(100))) = LOWER(@AssetNameItemKey) THEN 5
                ELSE 999
            END AS DisplayOrder
        FROM CompleteUserData cud
        LEFT JOIN MembershipData md ON md.UserId = cud.UserId
        CROSS APPLY OPENJSON(cud.MemberData) j
    ),
    OrderedJsonData AS (
        SELECT 
            jk.UploadedMemberDataId,
            jk.IsHorseExists,
            jk.UserId,
            jk.ProfilePicURL,
            jk.Mobile,
            jk.EmailAddress,
            jk.MembershipName,
            jk.MembershipTypeCount,
            '{' + STUFF((
                SELECT ',"' + JsonKey + '":"' + REPLACE(ISNULL(JsonValue, ''), '"', '\"') + '"'
                FROM JsonKeys jk2
                WHERE jk2.UploadedMemberDataId = jk.UploadedMemberDataId
                ORDER BY jk2.DisplayOrder, jk2.JsonKey
                FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)'), 1, 1, '') + '}' AS OrderedMemberData
        FROM JsonKeys jk
        GROUP BY 
            jk.UploadedMemberDataId, jk.IsHorseExists, jk.UserId,
            jk.ProfilePicURL, jk.Mobile, jk.EmailAddress, jk.MembershipName, jk.MembershipTypeCount
    )
    
    SELECT
        ojd.UploadedMemberDataId as Id,
        SUBSTRING(ojd.OrderedMemberData, 1, LEN(ojd.OrderedMemberData) - 1) + 
        ',"MemberImage":"' + 
            CASE 
                WHEN ojd.ProfilePicURL IS NULL OR ojd.ProfilePicURL = '' 
                    THEN '' 
                ELSE @BaseUrl + 'store/downloadPublic?f=' + REPLACE(ISNULL(ojd.ProfilePicURL, ''), '"', '\"') + '&t=user&p=1'
            END + '"' +
        ',"MemberStatus":""' +
        ',"Mobile":"' + REPLACE(ISNULL(ojd.Mobile, ''), '"', '\"') + '"' +
        ',"Email":"' + REPLACE(ISNULL(ojd.EmailAddress, ''), '"', '\"') + '"' +
        ',"MembershipName":"' + REPLACE(ISNULL(ojd.MembershipName, ''), '"', '\"') + '"' +
        ',"MembershipTypeCount":' + CAST(ISNULL(ojd.MembershipTypeCount, 0) AS NVARCHAR(10)) +
        '}' AS MemberData,
        ojd.IsHorseExists,
        CASE 
            WHEN ojd.UserId IS NULL THEN CAST(0 AS BIT)
            ELSE CAST(1 AS BIT)
        END AS IsMemberExists,
        CASE 
            WHEN ojd.UserId IS NULL AND ojd.IsHorseExists = 0 THEN 'Member does not exist,horse does not exists'
            WHEN ojd.UserId IS NULL THEN 'Member does not exist'
            WHEN ojd.IsHorseExists = 0 THEN 'Horse does not exists'
            ELSE ''
        END AS ValidationError
    FROM OrderedJsonData ojd;
    """;

        public async Task<MemberDetailsDto?> Handle(GetMemberDetailsByIdQuery request, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("Id", request.Id);
            var repo = readRepository.GetRepository<MemberDetailsDto>();
            var item = await repo.GetAsync(SqlQuery, cancellationToken, queryParameters, null, QueryType.Text);
            return item;
        }
    }
}
