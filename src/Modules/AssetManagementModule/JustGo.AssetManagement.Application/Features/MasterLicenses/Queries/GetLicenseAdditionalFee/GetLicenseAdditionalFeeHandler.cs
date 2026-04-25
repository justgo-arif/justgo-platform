using Dapper;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;


namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenseAdditionalFee
{
    public class GetLicenseAdditionalFeeHandler : IRequestHandler<GetLicenseAdditionalFeeQuery, List<AssetSurchargeDTO>>
    {
        private readonly LazyService<IReadRepository<AssetSurchargeDTO>> _readRepository;
        private IMediator _mediator;
        public GetLicenseAdditionalFeeHandler(LazyService<IReadRepository<AssetSurchargeDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<List<AssetSurchargeDTO>> Handle(GetLicenseAdditionalFeeQuery request, CancellationToken cancellationToken)
        {
            List<AssetSurchargeDTO> assetSurchargeDTOs = new List<AssetSurchargeDTO>();

            foreach (var item in request.LicenseIds)
            {

            int licenseDocId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { item.ToString() } }))[0];


            string sql = @"
                        DECLARE @OwnerId int = (select isnull(LicenceOwner,0) from License_Default where DocId =@LicenseDocId )
                        
                        IF EXISTS(SELECT * FROM AssetLicenseAdditionalFees where FeeLinkId = @LicenseDocId )
                        BEGIN
                        	   SELECT PD.DocId as ProductDocId,D.SyncGuid AS ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price FROM  AssetLicenseAdditionalFees ASTL
                        		INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                        		INNER JOIN Document D On D.DocId = PD.DocId WHERE ASTL.FeeLinkId = @LicenseDocId
                        END
                        ELSE
                        BEGIN
                        	 --IF EXISTS(select * from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
                        		--INNER JOIN AssetLicenseAdditionalFees ASTL on ASTL.FeeLinkType = HT.Id AND H.EntityId = 0)
                        	 --BEGIN
                        		select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
                        		INNER JOIN AssetLicenseAdditionalFees ASTL on ASTL.FeeLinkType = HT.Id
                        		INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                        		INNER JOIN Document D On D.DocId = PD.DocId AND H.EntityId = 0
                        	 --END
                        		UNION ALL
                        	 --IF EXISTS(select 1 from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
                        		--INNER JOIN AssetLicenseAdditionalFees ASTL on ASTL.FeeLinkType = HT.Id AND H.EntityId = @OwnerId)
                        	 --BEGIN
                        		select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
                        		INNER JOIN AssetLicenseAdditionalFees ASTL on ASTL.FeeLinkType = HT.Id
                        		INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                        		INNER JOIN Document D On D.DocId = PD.DocId AND H.EntityId = @OwnerId AND H.EntityId <> 0
                        	 --END
                        
                        END
                        ";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LicenseDocId", licenseDocId, dbType: DbType.Int32);
            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            assetSurchargeDTOs.AddRange(result);
        }
            return assetSurchargeDTOs;
        }
    }
}
