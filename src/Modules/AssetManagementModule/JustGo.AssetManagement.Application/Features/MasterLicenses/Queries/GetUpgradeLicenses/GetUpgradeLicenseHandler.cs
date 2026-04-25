using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetAssetLicenseById;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.EvaluateAssetPurchaseRule;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetMyAssets;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenses;
using JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetAssetMasterLicenses;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;
using System.Threading;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetUpgradeLicenses
{
    public class GetUpgradeLicenseHandler : IRequestHandler<GetUpgradeLicenseQuery, List<AssetMasterLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<AssetMasterLicenseDTO>> _readRepository;
        private readonly IMediator _mediator;
        public GetUpgradeLicenseHandler(LazyService<IReadRepository<AssetMasterLicenseDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<AssetMasterLicenseDTO>> Handle(GetUpgradeLicenseQuery request, CancellationToken cancellationToken)
        {
            string SQL = @"
                            declare @Classification nvarchar(255)=''
	                        declare @OwnerId int

                            declare @json nvarchar(max) = (	select AssetTypeConfig from AssetTypes where RecordGuid = @assetTypeId	)
                            select  @Classification = classification,@OwnerId = isnull(LicenceOwner,0) from License_Default LD INNER JOIN Document D on D.DocId = LD.DocId  where D.SyncGuid = @LicenseId
                            
                            ;with CTE_LicenseClassification AS(
                            SELECT 
                                --m.[key] AS LicenseType,
                                u.[value] AS UpgradeTo
                            FROM OPENJSON(@json, '$.LicenseUpgradeMap') AS m
                            OUTER APPLY OPENJSON(m.[value]) AS u
                            where m.[key] = @Classification
                            )
                            
                            SELECT 
							Ld.DocId as LicenseDocId,
							pd.DocId as ProductDocId,
                            d.Location,
                            ld.DocId AS LicenseDocId,
                            cast(D.SyncGuid as uniqueidentifier) AS LicenseId,
                            pd.DocId AS ProductDocId,
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
                            ATL.LicenseConfig AS LicenseConfig
                        FROM 
                            License_Default ld
                            --INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                            INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                            INNER JOIN Products_Default pd ON ll.Entityid = pd.DocId
                            INNER JOIN Document d ON d.DocId = ld.DocId
							INNER JOIN (select DocId, cast(SyncGuid as uniqueidentifier) as ProductId from Document ) P_Id on P_Id.DocId = pd.DocId
                            LEFT JOIN (select DocId, SyncGuid as OwnerId from Document ) C_Id on C_Id.DocId = ld.LicenceOwner                            
							INNER JOIN AssetTypesLicenseLink ATL ON ATL.LicenseDocId = ld.DocId and SourceUpgradeLicense > 0
							INNER JOIN AssetTypes AST ON AST.AssetTypeId = ATL.AssetTypeId
							INNER JOIN CTE_LicenseClassification  on CTE_LicenseClassification.UpgradeTo = ld.classification
                            LEFT JOIN AssetLicenseAdditionalFees fee on fee.FeeLinkId = ld.docid
							WHERE 
							AST.RecordGuid = @AssetTypeId AND ATL.LicenseType = @LicenseType AND ld.LicenceOwner = @OwnerId
						

                ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", request.AssetTypeId, dbType: DbType.Guid);
            queryParameters.Add("@LicenseType", request.LicenseType, dbType: DbType.Int32);
            queryParameters.Add("@LicenseId", request.LicenseId, dbType: DbType.Guid);
            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();

            return await EvaluateProductPurchaseRule(result, request, cancellationToken);

        }

        private async Task<List<AssetMasterLicenseDTO>> EvaluateProductPurchaseRule(List<AssetMasterLicenseDTO> assetMasterLicenses, GetUpgradeLicenseQuery request, CancellationToken cancellationToken)
        {
            int assetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { request.AssetRegisterId.ToString() } }))[0];

            var filteredList = new List<AssetMasterLicenseDTO>();

            foreach (var item in assetMasterLicenses)
            {
                if (item.LicenseConfig == null || string.IsNullOrEmpty(item.LicenseConfig))
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
