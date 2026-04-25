using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;


namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenses
{
    public class MasterLicensesHandler : IRequestHandler<GetMasterLicenses, List<AssetMetaDataMasterLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<AssetMetaDataMasterLicenseDTO>> _readRepository;
        private IMediator _mediator;
        private readonly IUtilityService _utilityService;
        public MasterLicensesHandler(LazyService<IReadRepository<AssetMetaDataMasterLicenseDTO>> readRepository, IMediator mediator, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
        }
        public async Task<List<AssetMetaDataMasterLicenseDTO>> Handle(GetMasterLicenses request, CancellationToken cancellationToken)
        {


            AssetTypeDto assetTypeResult = await _mediator.Send(new AssetMetadataQuery(request.AssetTypeId), cancellationToken);
            string licenseOwner = string.Join(",", assetTypeResult.AssetTypeConfig.LicenseOwners);
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            string SQL = $@"DECLARE @IsAdmin bit =
                                CASE
                                    WHEN EXISTS (
                                        SELECT 1
                                        FROM AbacUserRoles AUR
                                        INNER JOIN AbacRoles AR ON AR.Id = AUR.RoleId
                                        WHERE AR.Name IN ('System Admin','Asset Super Admin')
                                          AND AUR.UserId = @UserId
                                    ) THEN 1 ELSE 0
                                END;
                            
                            ;WITH MemberClubs AS
                            (
                                SELECT h.entityid
                                FROM hierarchylinks hl
                                INNER JOIN hierarchies h ON h.Id = hl.HierarchyId
                                -- hierarchytypes was not used; remove the join unless you need it for filtering
                                -- INNER JOIN hierarchytypes ht ON ht.id = h.hierarchytypeid
                                WHERE hl.UserId = @UserId
                            ),
                            Base AS
                            (
                                SELECT
                                    ld.DocId                              AS LicenseDocId,
                                    pd.DocId                              AS ProductDocId,
                                    pd.Location,
                                    CAST(dLic.SyncGuid AS uniqueidentifier) AS LicenseId,
                                    CAST(dProd.SyncGuid AS uniqueidentifier) AS ProductId,
                                    ld.Reference,
                                    ld.Sequence,
                                    ISNULL(cdoc.SyncGuid, '')             AS LicenceOwner,      
                                    ISNULL(pd.Code, '')                   AS Code,
                                    pd.Name                               AS ProductName,
                                    cd.ClubName                           AS LicenseOwnerName,
                                    cd.DocId                              AS ClubDocId
                                FROM License_Default             AS ld
                                INNER JOIN ProcessInfo           AS p    ON p.PrimaryDocId = ld.DocId
                                INNER JOIN License_Links         AS ll   ON ll.DocId = ld.DocId
                                INNER JOIN Products_Default      AS pd   ON pd.DocId = ll.EntityId
                                INNER JOIN Clubs_Default         AS cd   ON cd.DocId = ld.LicenceOwner
                                INNER JOIN Document              AS dLic ON dLic.DocId = ld.DocId
                                INNER JOIN Document              AS dProd ON dProd.DocId = pd.DocId
                                LEFT  JOIN Document              AS cdoc ON cdoc.DocId = ld.LicenceOwner
                                INNER JOIN AssetTypesLicenseLink AS ATL  ON ATL.LicenseDocId = ld.DocId
                                INNER JOIN AssetTypes            AS AST  ON AST.AssetTypeId = ATL.AssetTypeId
                                WHERE AST.RecordGuid = @AssetTypeId
                                  AND ATL.LicenseType = @LicenseType
                            )
                            
                            SELECT DISTINCT
                                b.LicenseDocId,
                                b.ProductDocId,
                                b.Location,
                                b.LicenseId,
                                b.ProductId,
                                b.Reference,
                                b.Sequence,
                                b.LicenceOwner,
                                b.Code,
                                b.ProductName,
                                b.LicenseOwnerName
                            FROM Base AS b
                            WHERE @IsAdmin = 1
                            
                            UNION ALL
                            
                            SELECT DISTINCT
                                b.LicenseDocId,
                                b.ProductDocId,
                                b.Location,
                                b.LicenseId,
                                b.ProductId,
                                b.Reference,
                                b.Sequence,
                                b.LicenceOwner,
                                b.Code,
                                b.ProductName,
                                b.LicenseOwnerName
                            FROM Base AS b
                            INNER JOIN MemberClubs AS mc
                                ON mc.entityid = b.ClubDocId
                            WHERE @IsAdmin = 0
						

                            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", request.AssetTypeId, dbType: DbType.Guid);
            queryParameters.Add("@LicenseType",request.LicenseType, dbType: DbType.Int32);
            queryParameters.Add("@LicenseOwner", licenseOwner, dbType: DbType.String);
            queryParameters.Add("@UserId", currentUserId, dbType: DbType.Int32);
            //queryParameters.Add("@OwnerId",request.OwnerId, dbType: DbType.Guid);
            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }

    }
}
