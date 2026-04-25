using Dapper;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.AssetAdditionalFeeByType
{
    public class GetAssetAdditionalFeeByTypeHandler : IRequestHandler<GetAssetAdditionalFeeByTypeQuery, List<AssetSurchargeDTO>>
    {
        private readonly LazyService<IReadRepository<AssetSurchargeDTO>> _readRepository;
        private IMediator _mediator;

        public GetAssetAdditionalFeeByTypeHandler(LazyService<IReadRepository<AssetSurchargeDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<AssetSurchargeDTO>> Handle(GetAssetAdditionalFeeByTypeQuery request, CancellationToken cancellationToken)
        {
            string sql = @"declare @OwnerId int = (select DocId from Document where SyncGuid = @OwnerSyncGuid)
                        select PD.DocId as ProductDocId,
                               D.SyncGuid as ProductId,
                               PD.Name,
                               ASTL.DisplayName,
                               ASTL.Value as Price 
                        from AssetTransactionFee ASTL 
                        INNER JOIN Products_Default PD on PD.DocId = ASTL.ProductId
                        INNER JOIN Document D On D.DocId = PD.DocId 
                        WHERE ASTL.FeeLinkId = @OwnerId and ASTL.Type = @TypeId";

            DynamicParameters queryParameters = new DynamicParameters();
            queryParameters.Add("@TypeId", request.Type, dbType: DbType.Int32);
            queryParameters.Add("@OwnerSyncGuid", request.OwnerId, dbType: DbType.String);

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            return result;
        }
    }
}