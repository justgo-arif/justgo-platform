using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetAssetLicenseById;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.FetchProductByTag;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.GetAssetPurchaseDiscountConfig;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenseAdditionalFee;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetSourceUpgradeLicenses;
using JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetAssetMasterLicenses;
using JustGo.AssetManagement.Application.Features.RuleHelper;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Mapster;
using Pipelines.Sockets.Unofficial.Buffers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetLicenseAdditionalFeeV2
{
    public class GetLicenseAdditionalFeeHandlerV2 : IRequestHandler<GetLicenseAdditionalFeeQueryV2, List<AssetSurchargeDTOV2>>
    {
        private readonly IReadRepositoryFactory _readDb;
        private IMediator _mediator;
        public GetLicenseAdditionalFeeHandlerV2(IMediator mediator, IReadRepositoryFactory read)
        {
            _mediator = mediator;
            _readDb = read;
        }
        public async Task<List<AssetSurchargeDTOV2>> Handle(GetLicenseAdditionalFeeQueryV2 request, CancellationToken cancellationToken)
        {
            List<AssetSurchargeDTOV2> assetSurchargeDTOs = new List<AssetSurchargeDTOV2>();
            List<AssetLicenseResultDTO> assetLicenseResultDTOs = await _mediator.Send(new GetAssetLicenseByIdQuery(request.AssetRegisterId.ToString()), cancellationToken);
            List<CartItem> cartItems = new List<CartItem>();

            var upgradeResult = await ProcessUpgradeLicenses(request.LicenseIds, assetLicenseResultDTOs, cancellationToken);
            assetSurchargeDTOs.AddRange(upgradeResult.Surcharges);
            cartItems.AddRange(upgradeResult.CartItems);

            var groupDiscounts = await ProcessGroupDiscounts(cartItems, request.AssetRegisterId, cancellationToken);
            assetSurchargeDTOs.AddRange(groupDiscounts);

            return assetSurchargeDTOs;
        }

        private async Task<(List<AssetSurchargeDTOV2> Surcharges, List<CartItem> CartItems)> ProcessUpgradeLicenses(
            string[] licenseIds,
            List<AssetLicenseResultDTO> assetLicenseResultDTOs,
            CancellationToken cancellationToken)
        {
            List<AssetSurchargeDTOV2> assetSurchargeDTOs = new List<AssetSurchargeDTOV2>();
            List<CartItem> cartItems = new List<CartItem>();

            foreach (var item in licenseIds)
            {
                int licenseDocId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { item.ToString() } }))[0];

                List<SourceUpgradeLicenseDTO> resultUpgradeLicenses = await _mediator.Send(new GetSourceUpgradeLicensesQueryV2(1, licenseDocId), cancellationToken);
                List<AssetSurchargeDTOV2> assetAdditionalFees = await AssetLicenseAdditionalFees(licenseDocId, cancellationToken);
                var destinationUpgradeLicense = resultUpgradeLicenses.FirstOrDefault(x => x.LicenseDocId == licenseDocId && x.Type .Equals("Upgrade") && assetLicenseResultDTOs.Any(a => a.ProductDocId == x.SourceProductDocId && a.LicenseStatus.ToLower()=="active"));
                var sourceUpgradeLicense = resultUpgradeLicenses.FirstOrDefault(x => x.LicenseDocId == destinationUpgradeLicense?.SourceLicenseDocId && x.Type.Equals("Source"));               
                bool sourceLicenseExists = resultUpgradeLicenses.Where(x => x.LicenseDocId == licenseDocId && x.Type .Equals("Upgrade")).ToList().Any(a => assetLicenseResultDTOs.Any((b => b.ProductDocId == a.SourceProductDocId && b.LicenseStatus.ToLower() == "active")));
 
                if (destinationUpgradeLicense != null && sourceUpgradeLicense != null && sourceLicenseExists)
                {
                    List<AssetSurchargeDTOV2> sourceAdditionalFees = await AssetLicenseAdditionalFees(destinationUpgradeLicense.SourceLicenseDocId, cancellationToken);
                    if (destinationUpgradeLicense != null && sourceUpgradeLicense != null && sourceLicenseExists)
                    {
                        var destItem = assetAdditionalFees.FirstOrDefault(e => e.Type .Equals("Discount") && e.OwnerId.Equals(destinationUpgradeLicense.OwnerId));
                        if (destItem != null)
                            assetSurchargeDTOs.Add(new AssetSurchargeDTOV2
                            {
                                ProductDocId = destItem.ProductDocId,
                                ProductId = destItem.ProductId,
                                Name = destItem.Name,
                                DisplayName = "Source Item Discount Offering",
                                Price = sourceUpgradeLicense.UnitPrice,
                                Type = "AssetDiscount",
                                ItemTag = "AssetDiscountFee|" + @"{""TargetProductId"":" + destinationUpgradeLicense.ProductDocId + @",""Discount"":" + sourceUpgradeLicense.UnitPrice + "}"
                            });
                    }

                    //add discount logic
                    foreach (var assetSurcharge in assetAdditionalFees.Where(e => e.Type != "Discount"))
                    {
                        var sourceItem = sourceAdditionalFees.FirstOrDefault(e => e.ProductDocId == assetSurcharge.ProductDocId && e.OwnerId == assetSurcharge.OwnerId && e.Type != "Discount");
                        if (sourceItem != null && assetSurcharge.Price >= sourceItem.Price)
                        {
                            var sourceDiscountItem = sourceAdditionalFees.FirstOrDefault(e => e.Type == "Discount" && e.OwnerId == assetSurcharge.OwnerId);
                            if (sourceDiscountItem != null)
                                assetSurchargeDTOs.Add(new AssetSurchargeDTOV2
                                {
                                    ProductDocId = sourceDiscountItem.ProductDocId,
                                    ProductId = sourceDiscountItem.ProductId,
                                    Name = sourceItem.Name + "-Fee",
                                    DisplayName = "Source Fee Discount",
                                    Price = sourceItem.Price,
                                    Type = "AssetDiscount",
                                    ItemTag = "AssetDiscountFee|" + @"{""TargetProductId"":" + sourceItem.ProductDocId + @",""Discount"":" + sourceItem.Price + "}"
                                });
                        }
                        assetSurchargeDTOs.Add(assetSurcharge);
                    }
                }
                else
                {
                    assetSurchargeDTOs.AddRange(assetAdditionalFees.Where(e => e.Type != "Discount"));
                }

                cartItems.Add(new CartItem() { LicenseDocId = licenseDocId });
            }

            return (assetSurchargeDTOs, cartItems);
        }

        private async Task<List<AssetSurchargeDTOV2>> ProcessGroupDiscounts(List<CartItem> cartItems, Guid assetRegisterId, CancellationToken cancellationToken)
        {
            var surchargeDtos = new List<AssetSurchargeDTOV2>();

            cartItems = await GetProductByLicenseIds(cartItems, assetRegisterId, cancellationToken);

            foreach (var item in cartItems.GroupBy(x => x.OwnerId).Select(g => g.First()))
            {

                var ruleConfig = await _mediator.Send(new GetAssetPurchaseDiscountConfigQuery(item.OwnerId), cancellationToken);

                if (ruleConfig == null || string.IsNullOrEmpty(ruleConfig.RuleConfig))
                    continue;

                AssetPurchaseDiscountConfig assetPurchaseDiscountConfig = JsonSerializer.Deserialize<AssetPurchaseDiscountConfig>(ruleConfig.RuleConfig);

                if (assetPurchaseDiscountConfig?.DiscountRules == null || assetPurchaseDiscountConfig.LicenseGroups == null)
                    continue;

                var calculatedDiscount = AssetDiscountRuleEngine.CalculateCartDiscount(
                       cartItems.Where(e => e.OwnerId == item.OwnerId).ToList(),
                       assetPurchaseDiscountConfig.LicenseGroups,
                       assetPurchaseDiscountConfig.DiscountRules);

                if (calculatedDiscount?.CartItems?.Any() != true || calculatedDiscount.Amount <= 0)
                {
                    continue;
                }

                var firstDiscountCartItem = calculatedDiscount.CartItems.FirstOrDefault();
                var additionalFees = await _mediator.Send(
                         new FetchProductByTagQuery
                         {
                             ProductTag = ProductTag.Surcharge.ToString(),
                             OwnerId = item.OwnerId,
                         },
                         cancellationToken);



                if (additionalFees != null)
                {
                    var productIds = string.Join(",",
                        calculatedDiscount.CartItems.Select(e => e.ProductDocId));

                    surchargeDtos.Add(new AssetSurchargeDTOV2
                    {
                        ProductDocId = additionalFees.ProductDocId,
                        ProductId = additionalFees.ProductId,
                        Name = additionalFees.Name,
                        DisplayName = "Group Fee Discount",
                        Price = calculatedDiscount.Amount,
                        Type = "AssetDiscount",
                        ItemTag = $"AssetGroupFee|{{\"GroupDiscountProductId\":\"{productIds}\",\"Discount\":{calculatedDiscount.Amount}}}"
                    });
                }
            }
            return surchargeDtos;
        }

        private async Task<List<AssetSurchargeDTOV2>> AssetLicenseAdditionalFees(int licenseDocId, CancellationToken cancellationToken)
        {
            string sql = @"DECLARE @OwnerId int = (select isnull(LicenceOwner,0) from License_Default where DocId =@LicenseDocId )

                        IF EXISTS(SELECT * FROM AssetLicenseAdditionalFees where FeeLinkId = @LicenseDocId )
                        BEGIN
                              	   SELECT PD.DocId as ProductDocId,D.SyncGuid AS ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price,'' Type,ISNULL(PD.Ownerid,0) as OwnerId FROM  AssetLicenseAdditionalFees ASTL
                                    		INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                                    		INNER JOIN Document D On D.DocId = PD.DocId WHERE ASTL.FeeLinkId = @LicenseDocId
                              		
                              		UNION
                              		select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,'' DisplayName,0 as Price,Producttag Type,ISNULL(PD.Ownerid,0) as OwnerId from Products_Default PD
                                    				inner join Document D on D.DocId = PD.Docid where   Producttag ='Discount' 
                        							and PD.Ownerid = ( SELECT ISNULL(LicenceOwner,0)  FROM  License_Default WHERE DocId= @LicenseDocId)
									UNION

									select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,'' DisplayName,0 as Price,Producttag Type,ISNULL(PD.Ownerid,0) as OwnerId from Products_Default PD
                                    				inner join Document D on D.DocId = PD.Docid where   Producttag ='Discount' 
                        							and PD.Ownerid = (select ISNULL(PD.Ownerid,0) FROM  AssetLicenseAdditionalFees ASTL
                                    				INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                                    				INNER JOIN Document D On D.DocId = PD.DocId WHERE ASTL.FeeLinkId = @LicenseDocId)
                        END
                        ELSE
                        BEGIN
                        
                              select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price,'' Type,ISNULL(PD.Ownerid,0) as OwnerId from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
                              INNER JOIN AssetLicenseAdditionalFees ASTL on ASTL.FeeLinkType = HT.Id
                              INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                              INNER JOIN Document D On D.DocId = PD.DocId AND H.EntityId = 0
                              
                        	  UNION ALL
                              
                        	  select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price,'' Type,ISNULL(PD.Ownerid,0) as OwnerId from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
                              INNER JOIN AssetLicenseAdditionalFees ASTL on ASTL.FeeLinkType = HT.Id
                              INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                              INNER JOIN Document D On D.DocId = PD.DocId AND H.EntityId = @OwnerId AND H.EntityId <> 0
                              
                        	 UNION ALL
                             
                        	 select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,'' DisplayName,0 as Price,Producttag Type,ISNULL(PD.Ownerid,0) as OwnerId from Products_Default PD
                              inner join Document D on D.DocId = PD.Docid where ownerid = @OwnerId and Producttag ='Discount'
                        	 
                        	 UNION ALL
                              
                        	  select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,'' DisplayName,0 as Price,Producttag Type,ISNULL(PD.Ownerid,0) as OwnerId from Products_Default PD
                                       					inner join Document D on D.DocId = PD.Docid where ownerid = 0 and Producttag ='Discount'
                        END
                        ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LicenseDocId", licenseDocId, dbType: DbType.Int32);
            var result = await _readDb.GetLazyRepository<AssetSurchargeDTOV2>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return result.ToList();
        }

        private async Task<List<CartItem>> GetProductByLicenseIds(List<CartItem> licenseDocId, Guid assetRegisterId, CancellationToken cancellationToken)
        {
            string sql = @"declare @AssetId int = (select  top 1 AssetId from Assetregisters where RecordGuid = @AssetRegisterId)
                            select LL.Docid as LicenseDocId,Pd.DocId as ProductDocId,ISNULL(pd.Ownerid,0) OwnerId from License_Links LL Inner join Products_Default PD on Pd.DocId = LL.Entityid
                            inner join AssetLicenses AL on Al.ProductId = PD.DocId and AL.AssetId = @AssetId and Al.StatusId = 12
                            where LL.DocId in (Select value from string_split(@LicenseDocIds,',') )
                        ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LicenseDocIds", string.Join(",", licenseDocId.Select(e => e.LicenseDocId)), dbType: DbType.String);
            queryParameters.Add("@AssetRegisterId", assetRegisterId, dbType: DbType.Guid);
            var result = await _readDb.GetLazyRepository<CartItem>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return result.ToList();
        }
    }
}
