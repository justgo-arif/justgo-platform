using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;


namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyMemberMemberships
{
    public class GetFamilyMemberMembershipsHandler : IRequestHandler<GetFamilyMemberMembershipsQuery, List<OrganisationType>>
    {
        private readonly LazyService<IReadRepository<List<OrganisationType>>> _readRepository;
        public GetFamilyMemberMembershipsHandler(LazyService<IReadRepository<List<OrganisationType>>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<OrganisationType>> Handle(GetFamilyMemberMembershipsQuery request, CancellationToken cancellationToken = default)
        {
            string sql = """
                DECLARE @Organisation NVARCHAR(100)
                DECLARE @RegionalType NVARCHAR(50)
                DECLARE @SubRegionalType NVARCHAR(50)
                DECLARE @HOSTSYSTEMID NVARCHAR(50)
                DECLARE @OrganisationPic NVARCHAR(100)

                SELECT @HOSTSYSTEMID=[Value] FROM SystemSettings WHERE ItemKey LIKE 'CLUBPLUS.HOSTSYSTEMID'
                SELECT @OrganisationPic=[Value] FROM SystemSettings WHERE ItemKey LIKE 'ORGANISATION.LOGO'


                DECLARE @Hierarchy TABLE (OrganisationTypeName NVARCHAR(100),[Sequence] INT)
                DECLARE @ClubTypes TABLE (RowId INT,ClubType NVARCHAR(100))

                DECLARE @Sql varchar(5000) = (SELECT dbo.GetLookupTableQuery('Club Type'))
                INSERT INTO @ClubTypes(RowId,ClubType) EXECUTE(@Sql) 

                SELECT TOP 1 @Organisation= [Value]  FROM SystemSettings WHERE ItemKey = 'ORGANISATION.NAME'
                AND [Value] IS NOT NULL AND LTRIM(RTRIM([Value])) != '' 

                SELECT TOP 1 @RegionalType= [Value] FROM SystemSettings WHERE ItemKey ='ORGANISATION.REGIONAL_ENTITY_IDENTITY' 
                AND [Value] IS NOT NULL AND LTRIM(RTRIM([Value])) != ''  

                SELECT TOP 1 @SubRegionalType= [Value] FROM SystemSettings WHERE ItemKey ='ORGANISATION.SUB_REGIONAL_ENTITY_IDENTITY' 
                AND [Value] IS NOT NULL AND LTRIM(RTRIM([Value])) != '' 


                INSERT INTO @Hierarchy (OrganisationTypeName,[Sequence])
                SELECT 'NGB',1 UNION
                SELECT @RegionalType,2 WHERE @RegionalType IS NOT NULL UNION
                SELECT @SubRegionalType,3 WHERE @SubRegionalType IS NOT NULL UNION
                SELECT ClubType,4 FROM @ClubTypes WHERE ClubType NOT IN ('NGB',ISNULL(@RegionalType,''),ISNULL(@SubRegionalType,''))


                SELECT h.*
                ,h.OrganisationTypeName [OwnerType]
                ,@ORGANISATION [OrganisationName]
                ,'/Store/Download?f='+@OrganisationPic+'&t=OrganizationLogo' OrganisationPicUrl
                ,@ORGANISATION [OwnerName]
                ,um.*
                ,pd.Field_408 [MembershipName]
                ,s.name MembershipStatus,pr.LastActiondate 
                FROM UserMemberships um
                INNER JOIN [User] u ON u.UserId=um.UserId
                INNER JOIN ProcessInfo pr ON pr.PrimaryDocId=um.MemberLicenseDocId
                INNER JOIN [State] s ON s.StateId=pr.CurrentStateId
                INNER JOIN [Document_11_63] pd ON pd.DocId=um.ProductId 
                INNER JOIN @Hierarchy h ON h.OrganisationTypeName='NGB'
                WHERE u.UserSyncId = @MemberSyncGuId
                AND ISNULL(um.LicenceOwner,0)=0

                UNION

                SELECT CASE WHEN h.[Sequence]=4 THEN 'Club' ELSE h.OrganisationTypeName END OrganisationType,h.[Sequence]
                ,h.OrganisationTypeName [OwnerType]
                ,cd.Field_40 [OrganisationName]
                ,'/store/download?f='+d.[Location]+'&t=repo&p='+CAST(cd.DocId AS NVARCHAR(50))+'&p1=&p2=2' OrganisationPicUrl
                ,cd.Field_40 [OwnerName]
                ,um.*
                ,pd.Field_408 [MembershipName]
                ,s.[Name] MembershipStatus,pr.LastActiondate 
                FROM UserMemberships um
                INNER JOIN [User] u on u.UserId=um.UserId
                INNER JOIN ProcessInfo pr on pr.PrimaryDocId=um.MemberLicenseDocId
                INNER JOIN [State] s on s.StateId=pr.CurrentStateId
                INNER JOIN [Document_11_63] pd ON pd.DocId=um.ProductId 
                INNER JOIN Document_2_7 cd on cd.DocId=um.LicenceOwner
                INNER JOIN Document d on d.DocId=cd.DocId
                LEFT JOIN @Hierarchy h on h.OrganisationTypeName = cd.Field_70
                WHERE u.UserSyncId = @MemberSyncGuId
                ORDER BY h.[Sequence],OrganisationTypeName

                """;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@MemberSyncGuId", request.Id);

            var aggregated = new List<OrganisationType>();

            // Perform multi-mapping; the repository will return a flat list we ignore, using our own aggregation.
            _ = await _readRepository.Value.GetListMultiMappingAsync<OrganisationType, Organisation, UserMembership>(
                sql,
                cancellationToken,
                "OrganisationTypeName",
                (orgType, org, membership) =>
                {
                    if (orgType == null || string.IsNullOrWhiteSpace(orgType.OrganisationTypeName))
                        return orgType;

                    // Map OrganisationType.OrganisationType to Organisation.OwnerType
                    org.OwnerType = orgType.OrganisationTypeName;

                    // Map Organisation.OrganisationName to UserMembership.OwnerName
                    if (membership != null)
                        membership.OwnerName = org.OrganisationName;

                    var existingType = aggregated.FirstOrDefault(x => x.OrganisationTypeName == orgType.OrganisationTypeName);
                    if (existingType == null)
                    {
                        existingType = orgType;
                        existingType.Organisations ??= new List<Organisation>();
                        aggregated.Add(existingType);
                    }

                    if (org != null && !string.IsNullOrWhiteSpace(org.OrganisationName))
                    {
                        var existingOrg = existingType.Organisations.FirstOrDefault(o => o.OrganisationName == org.OrganisationName);
                        if (existingOrg == null)
                        {
                            existingOrg = org;
                            existingOrg.UserMemberships ??= new List<UserMembership>();
                            existingType.Organisations.Add(existingOrg);
                        }

                        if (membership != null && membership.Id != 0)
                        {
                            existingOrg.UserMemberships ??= new List<UserMembership>();
                            existingOrg.UserMemberships.Add(membership);
                        }
                    }

                    return existingType;
                },
                queryParameters,
                dbTransaction: null,
                splitOn: "OrganisationType,OwnerType,OwnerName",
                commandType: "text"
            );

            return aggregated;
        }
    }
}
