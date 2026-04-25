using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetAssetLicenseById;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.EvaluateAssetPurchaseRule;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenses;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetSourceUpgradeLicenses;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetAssetMasterLicenses
{
    public class GetAssetMasterLicenseHandler : IRequestHandler<GetAssetMasterLicenseQuery, List<AssetMasterLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<AssetMasterLicenseDTO>> _readRepository;
        private IMediator _mediator;
        private readonly IUtilityService _utilityService;
        public GetAssetMasterLicenseHandler(LazyService<IReadRepository<AssetMasterLicenseDTO>> readRepository, IMediator mediator, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
        }
        public async Task<List<AssetMasterLicenseDTO>> Handle(GetAssetMasterLicenseQuery request, CancellationToken cancellationToken)
        {
            string cteConditionSQL = "";
            //AssetTypeDto assetTypeResult = await _mediator.Send(new AssetMetadataQuery(request.AssetTypeId), cancellationToken);
            string licenseOwner = "";//string.Join(",", assetTypeResult.AssetTypeConfig.LicenseOwners);
            //int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            string conditionSQL = !string.IsNullOrEmpty(licenseOwner) ? " INNER JOIN cte_memberLicense  on cte_memberLicense.OwnerId = ld.LicenceOwner " : " INNER JOIN cte_memberclub cte_memberclub ON cte_memberclub.EntityId = ld.LicenceOwner ";
            string conditionSQLUnion = !string.IsNullOrEmpty(licenseOwner) ? " INNER JOIN cte_memberLicense  on cte_memberLicense.OwnerId = ld.LicenceOwner " : " INNER JOIN cte_memberclub cte_memberclub ON cte_memberclub.EntityId = ld.LicenceOwner ";
            //string conditionSQLcte_memberclub = !string.IsNullOrEmpty(licenseOwner) ? " AND TRIM(ht.hierarchytypename) IN (SELECT TRIM(value) FROM string_split(@LicenseOwner, ',')) " : "  ";
            string conditionSQLcte_memberclub = !string.IsNullOrEmpty(licenseOwner) ? " INNER JOIN hierarchytypes hierarchytypes on hierarchytypes.Id = Hierarchies.HierarchyTypeId AND hierarchytypes.HierarchyTypeName in ((SELECT TRIM(value) FROM string_split(@LicenseOwner, ','))) " : "  ";
            if (request.LicenseType == 2)
            {
                conditionSQL += @" INNER JOIN cte_validclassification VC ON VC.Classification = ld.Classification /*and VC.licenceowner = ld.licenceowner  */ ";
                //conditionSQLUnion += @" INNER JOIN cte_memberclub cte_memberclub ON cte_memberclub.EntityId = ld.LicenceOwner ";
                cteConditionSQL = @" , cte_validclassification AS
            (
           SELECT     ld.classification,ld.licenceowner
           FROM       assetlicenses al
           INNER JOIN products_links pl
           ON         pl.docid = al.productid
           INNER JOIN assettypeslicenselink atl
           ON         atl.licensedocid = pl.entityid
           INNER JOIN license_default ld
           ON         ld.docid = atl.licensedocid
		   --Inner JOIN AssetRegisters ar
		   --ON		ar.AssetId=al.AssetId			
           WHERE      atl.licensetype = 1
           AND        al.AssetId = @AssetId
           AND        NOT EXISTS
                      (SELECT 1
                             FROM   assetstatus AS s
                             WHERE  s.assetstatusid = al.statusid
                             AND    s.NAME IN ('Expired',
                                               'Suspended') ))";
            }

            string SQL = $@"
                            declare @AssetId int = (select top 1 AssetId from AssetRegisters where RecordGuid = @AssetRegisterId )


                            ;WITH cte_memberclub AS
                            (
                                SELECT h.entityid
                                FROM hierarchylinks hl
                                INNER JOIN hierarchies h ON h.Id = hl.HierarchyId
                                --INNER JOIN hierarchytypes ht ON ht.id = h.hierarchytypeid
                                INNER JOIN AssetOwners AO ON AO.OwnerId = hl.UserId AND AO.AssetId = @AssetId
                               /* WHERE NOT EXISTS (
                                    SELECT 1
                                    FROM AssetOwners ao_check
                                    WHERE ao_check.AssetId = @AssetId
                                    AND NOT EXISTS (
                                        SELECT 1
                                        FROM UserMemberships UM
	                            		INNER JOIN Hierarchies on Hierarchies.EntityId = ISNULL(UM.LicenceOwner, 0)
	                            		{conditionSQLcte_memberclub}
                                        WHERE UM.StatusId = 62
                                        AND UM.UserId = ao_check.OwnerId
                                        --AND ISNULL(UM.LicenceOwner, 0) = 0
                                    )
                                )*/
)
                            ,
                            cte_memberLicense AS
                            (
                                --SELECT ISNULL(UserMemberships.LicenceOwner,0) as OwnerId
                                --FROM UserMemberships
                                --INNER JOIN ProcessInfo PR ON PR.primaryDocId = UserMemberships.MemberLicenseDocId  
	                            --INNER JOIN cte_memberclub CM on CM.EntityId = ISNULL(UserMemberships.LicenceOwner,0)
	                           -- where UserMemberships.UserId = @UserId AND PR.CurrentStateId = 62
                                select entityid as OwnerId from   cte_memberclub where entityid >0
                            )
                            {cteConditionSQL}

                            ,cte_allassetlicenses AS
                            (
                            SELECT DISTINCT
                            Ld.DocId as LicenseDocId,
                            pd.DocId as ProductDocId,
                            d.Location,
                            cast(D.SyncGuid as uniqueidentifier) AS LicenseId,
                            P_Id.ProductId,
                            ld.Reference,
                            ld.Benefits,
                            ld.[Description],
                            ld.HidePrice,
                            ld.Sequence,
                            ISNULL(c_Id.OwnerId,'')  as LicenceOwner,
                            ISNULL(pd.Code,'') AS Code,
                            pd.Name AS ProductName,
                            '' AS ProductDescription,
                            pd.Category,
                            pd.Unitprice + isnull(fee.Value,0) AS Price,
                            pd.Currency,
                            pd.Availablequantity,
                            ISNULL(pd.Color,'') AS ProductColor,
                            ld.RenewalWindow AS LicenseType,
                            ld.ExpiryDateEndingUnit,
                            ld.ExpiryDateEndingValue,
                            ld.HideMembershipDuration,
                            ld.HideViewMoreAboutMembership,
                            ld.Classification,
                            ld.PriceOption,
                            ld.FromPrice,
                            ld.ToPrice,
                            ld.MembershipJourney,
                            ld.AlternateDisplayCurrency,
                            pd.IsSubscriptionEnabled,
                            pd.Recurringdescription,
                            pd.RecurringMandatory,
                            0 AS UpgradeType,
                            0 AS UpgradeId,
                            Expirydatestartingtype AS Expirydatestartingtype,
                            Expirydatestartingvalue AS Expirydatestartingvalue,
                            ATL.LicenseConfig AS LicenseConfig,
							ATL.LicenseLinkId
                        FROM 
                            License_Default ld
                            INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                            INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                            INNER JOIN Products_Default pd ON ll.Entityid = pd.DocId
                            INNER JOIN Document d ON d.DocId = ld.DocId
                            INNER JOIN (select DocId, cast(SyncGuid as uniqueidentifier) as ProductId from Document ) P_Id on P_Id.DocId = pd.DocId
                            LEFT JOIN (select DocId, SyncGuid as OwnerId from Document ) C_Id on C_Id.DocId = ld.LicenceOwner                            
                            INNER JOIN AssetTypesLicenseLink ATL ON ATL.LicenseDocId = ld.DocId
                            INNER JOIN AssetTypes AST ON AST.AssetTypeId = ATL.AssetTypeId
                            LEFT JOIN AssetLicenseAdditionalFees fee on fee.FeeLinkId = ld.docid
                            {conditionSQL}
                            WHERE 
                            AST.RecordGuid = @AssetTypeId AND ATL.LicenseType = @LicenseType

                            UNION

							SELECT DISTINCT
                            Ld.DocId as LicenseDocId,
                            pd.DocId as ProductDocId,
                            d.Location,
                            cast(D.SyncGuid as uniqueidentifier) AS LicenseId,
                            P_Id.ProductId,
                            ld.Reference,
                            ld.Benefits,
                            ld.[Description],
                            ld.HidePrice,
                            ld.Sequence,
                            ISNULL(c_Id.OwnerId,'')  as LicenceOwner,
                            ISNULL(pd.Code,'') AS Code,
                            pd.Name AS ProductName,
                            '' AS ProductDescription,
                            pd.Category,
                            pd.Unitprice + isnull(fee.Value,0) AS Price,
                            pd.Currency,
                            pd.Availablequantity,
                            ISNULL(pd.Color,'') AS ProductColor,
                            ld.RenewalWindow AS LicenseType,
                            ld.ExpiryDateEndingUnit,
                            ld.ExpiryDateEndingValue,
                            ld.HideMembershipDuration,
                            ld.HideViewMoreAboutMembership,
                            ld.Classification,
                            ld.PriceOption,
                            ld.FromPrice,
                            ld.ToPrice,
                            ld.MembershipJourney,
                            ld.AlternateDisplayCurrency,
                            pd.IsSubscriptionEnabled,
                            pd.Recurringdescription,
                            pd.RecurringMandatory,
                            0 AS UpgradeType,
                            0 AS UpgradeId,
                            Expirydatestartingtype AS Expirydatestartingtype,
                            Expirydatestartingvalue AS Expirydatestartingvalue,
                            ATL.LicenseConfig AS LicenseConfig,
							ATL.LicenseLinkId
                        FROM 
                            License_Default ld
                            --INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                            INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                            INNER JOIN Products_Default pd ON ll.Entityid = pd.DocId
                            INNER JOIN Document d ON d.DocId = ld.DocId
                            INNER JOIN (select DocId, cast(SyncGuid as uniqueidentifier) as ProductId from Document ) P_Id on P_Id.DocId = pd.DocId
                            LEFT JOIN (select DocId, SyncGuid as OwnerId from Document ) C_Id on C_Id.DocId = ld.LicenceOwner                            
                            INNER JOIN AssetTypesLicenseLink ATL ON ATL.LicenseDocId = ld.DocId
                            INNER JOIN AssetTypes AST ON AST.AssetTypeId = ATL.AssetTypeId
                            LEFT JOIN AssetLicenseAdditionalFees fee on fee.FeeLinkId = ld.docid
                            {conditionSQLUnion}
                            --INNER JOIN cte_validclassification VC ON VC.Classification = ld.Classification 
                            WHERE 
                            AST.RecordGuid = @AssetTypeId AND ATL.LicenseType = @LicenseType AND ISNULL(ld.Classification,'') = '')

                           SELECT  * from cte_allassetlicenses ORDER BY Sequence

						

                            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", request.AssetTypeId, dbType: DbType.Guid);
            queryParameters.Add("@LicenseType", request.LicenseType, dbType: DbType.Int32);
            queryParameters.Add("@LicenseOwner", licenseOwner, dbType: DbType.String);
            //queryParameters.Add("@UserId", currentUserId, dbType: DbType.Int32);
            queryParameters.Add("@AssetRegisterId", request.AssetRegisterId, dbType: DbType.Guid);
            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
            //return await EvaluateProductPurchaseRule(result, request, cancellationToken);
        }

        private async Task<List<AssetMasterLicenseDTO>> EvaluateProductPurchaseRule(List<AssetMasterLicenseDTO> assetMasterLicenses, GetAssetMasterLicenseQuery request, CancellationToken cancellationToken)
        {
            int assetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { request.AssetRegisterId.ToString() } }))[0];

            var filteredList = new List<AssetMasterLicenseDTO>();
            
            foreach (var item in assetMasterLicenses)
            {
                if(item.LicenseConfig == null || string.IsNullOrEmpty(item.LicenseConfig))
                {
                    filteredList.Add(item);
                    continue;
                }
                var evalResult = await _mediator.Send(
                    new EvaluateAssetPurchaseRuleQuery(
                        item.ProductDocId,
                        1,
                        assetId
                    ),
                    cancellationToken);

                if (evalResult != null && evalResult.IsEligible)
                {
                    filteredList.Add(item);
                }
            }

            return filteredList.OrderBy(e => e.Sequence).ToList();
        }

    }
}
