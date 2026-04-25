using Dapper;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetCartItems
{
    public class GetAssetCartItemsqueryHandler : IRequestHandler<GetAssetCartItemsQuery, bool>
    {
        private readonly LazyService<IReadRepository<AssetShoppingCartItemDTO>> _readRepository;
        private readonly IUtilityService _utilityService;
        private IMediator _mediator;
        public GetAssetCartItemsqueryHandler(LazyService<IReadRepository<AssetShoppingCartItemDTO>> readRepository, IMediator mediator, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
        }
        public async Task<bool> Handle(GetAssetCartItemsQuery request, CancellationToken cancellationToken)
        {

            int productId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { request.ProductId.ToString() } }))[0];
            int assetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { request.AssetRegisterId.ToString() } }))[0];
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            string sql = $@"   DELETE FROM  ShoppingCart_items where Forentityid =  @AssetId
                               select DocId  from ShoppingCart_items WHERE Productid = @ProductId AND Purchaseditemtag like '%Asset|%'
                                AND JSON_VALUE( REPLACE(  NULLIF(Purchaseditemtag, '') COLLATE DATABASE_DEFAULT, 'Asset|', '' ),'$.AssetGuid' ) = @AssetGuid

                                DELETE SCI
                                    FROM ShoppingCart_Items SCI
                                    INNER JOIN ShoppingCart_Default SCD ON SCI.DocId = SCD.DocId
                                    WHERE SCD.UserId = @UserId AND SCI.Purchaseditemtag like '%asset%'
                                ";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetGuid", request.AssetRegisterId, dbType: DbType.Guid);
            queryParameters.Add("@AssetId", assetId, dbType: DbType.Int32);
            queryParameters.Add("@ProductId", productId, dbType: DbType.Int32);
            queryParameters.Add("@UserId", currentUserId, dbType: DbType.Int32);
            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            if(result.Count > 0 )
                return false;
            else return true;
        }
    }
}
