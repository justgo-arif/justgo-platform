using Dapper;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Commands.RemoveAssetCartItems
{
    public class RemoveAssetCartItemsCommandHandler : IRequestHandler<RemoveAssetCartItemsCommand, bool>
    {
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly LazyService<IReadRepository<AssetShoppingCartItemDTO>> _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IMediator _mediator;

        public RemoveAssetCartItemsCommandHandler(
            IWriteRepositoryFactory writeRepository,
            LazyService<IReadRepository<AssetShoppingCartItemDTO>> readRepository,
            IMediator mediator,
            IUtilityService utilityService)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(RemoveAssetCartItemsCommand request, CancellationToken cancellationToken)
        {
            int productId = (await _mediator.Send(new GetIdByGuidQuery() 
            { 
                Entity = AssetTables.Document, 
                RecordGuids = [request.ProductId.ToString()] 
            }))[0];

            int assetId = (await _mediator.Send(new GetIdByGuidQuery() 
            { 
                Entity = AssetTables.AssetRegisters, 
                RecordGuids = [request.AssetRegisterId.ToString()] 
            }))[0];

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            string checkExistenceSql = @"
                SELECT DocId 
                FROM ShoppingCart_items 
                WHERE Productid = @ProductId 
                    AND Purchaseditemtag LIKE '%Asset|%'
                    AND JSON_VALUE(REPLACE(NULLIF(Purchaseditemtag, '') COLLATE DATABASE_DEFAULT, 'Asset|', ''), '$.AssetGuid') = @AssetGuid";

            string deleteByAssetSql = @"
                DELETE FROM ShoppingCart_items  WHERE Forentityid = @AssetId
                DELETE from ShoppingCart_items WHERE Productid = @ProductId AND Purchaseditemtag like '%AssetLease|%'
                 AND JSON_VALUE( REPLACE(  NULLIF(Purchaseditemtag, '') COLLATE DATABASE_DEFAULT, 'AssetLease|', '' ),'$.AssetGuid' ) = @AssetGuid
";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetGuid", request.AssetRegisterId, dbType: DbType.Guid);
            queryParameters.Add("@AssetId", assetId, dbType: DbType.Int32);
            queryParameters.Add("@ProductId", productId, dbType: DbType.Int32);
            queryParameters.Add("@UserId", currentUserId, dbType: DbType.Int32);


            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync( deleteByAssetSql, queryParameters, null, "text");

            //var existingItems = (await _readRepository.Value.GetListAsync( checkExistenceSql, cancellationToken,  queryParameters,  null, "text")).ToList();

            //if (existingItems.Count == 0)
            //{
            //    return true;
            //}

            return true;
        }
    }
}
