using Dapper;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.LeaseAdditionalFees
{
    public class GetLeaseAdditionalFeeHandler : IRequestHandler<GetLeaseAdditionalFeeQuery, List<AssetSurchargeDTO>>
    {
        private readonly LazyService<IReadRepository<AssetSurchargeDTO>> _readRepository;
        private IMediator _mediator;

        public GetLeaseAdditionalFeeHandler(LazyService<IReadRepository<AssetSurchargeDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<AssetSurchargeDTO>> Handle(GetLeaseAdditionalFeeQuery request, CancellationToken cancellationToken)
        {
            //List<AssetSurchargeDTO> assetSurchargeDTOs = new List<AssetSurchargeDTO>();

            //string sql = @"IF EXISTS(SELECT * FROM AssetLeaseAdditionalFee WHERE FeeLinkId = @LeaseId)
            //    BEGIN
            //        SELECT 
            //            PD.DocId AS ProductDocId,
            //            D.SyncGuid AS ProductId,
            //            PD.Name,
            //            ALAF.DisplayName,
            //            ALAF.Value AS Price 
            //        FROM AssetLeaseAdditionalFee ALAF
            //        INNER JOIN Products_Default PD ON PD.DocId = ALAF.ProductId
            //        INNER JOIN Document D ON D.DocId = PD.DocId 
            //        WHERE ALAF.FeeLinkId = @LeaseId
            //    END
            //    ELSE
            //    BEGIN

            //        		select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
            //        		INNER JOIN AssetLeaseAdditionalFee ASTL on ASTL.FeeLinkType = HT.Id
            //        		INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
            //        		INNER JOIN Document D On D.DocId = PD.DocId AND H.EntityId = 0
            //        		--UNION ALL

            //        		--select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price from Hierarchies H INNER JOIN HierarchyTypes HT on HT.Id= H.HierarchyTypeId
            //        		--INNER JOIN AssetLeaseAdditionalFee ASTL on ASTL.FeeLinkType = HT.Id
            //        		--INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
            //        		--INNER JOIN Document D On D.DocId = PD.DocId AND H.EntityId = @OwnerId AND H.EntityId <> 0
            //    END";

            string sql = @"declare @OwnerId int =  (select DocId from Document where SyncGuid = @OwnerSyncGuid)
                        select PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,ASTL.DisplayName,ASTL.Value as Price 
                        from  AssetTransactionFee ASTL 
                            		INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                            		INNER JOIN Document D On D.DocId = PD.DocId AND ASTL.FeeLinkId = @OwnerId  and ASTL.type = 2";
                        
                DynamicParameters queryParameters = new DynamicParameters();
                queryParameters.Add("@LeaseId", request.LeaseId, dbType: DbType.String);
                queryParameters.Add("@OwnerSyncGuid", request.OwnerId, dbType: DbType.String);

                var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
               // assetSurchargeDTOs.AddRange(result);
            

            return result;
        }
    }
}
