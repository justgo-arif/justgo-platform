using Dapper;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.IsExistsLeaseCartItem;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.IsExistsLeaseCartItem
{
    public class IsExistsLeaseCartItemHandler : IRequestHandler<IsExistsLeaseCartItemQuery, bool>
    {
        private readonly LazyService<IReadRepository<AssetShoppingCartItemDTO>> _readRepository;

        public IsExistsLeaseCartItemHandler(LazyService<IReadRepository<AssetShoppingCartItemDTO>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<bool> Handle(IsExistsLeaseCartItemQuery request, CancellationToken cancellationToken)
        {
            //int productId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { request.ProductId.ToString() } }))[0];

            string sql = $@"declare @LeaseId int = (select AssetLeaseId  from AssetLeases where recordGuid = @EntityId)
                                select DocId  from ShoppingCart_items WHERE Forentityid = @LeaseId";
            if(request.EntityType == 2)
            {
                sql = $@"declare @LeaseId int = (select AssetLeaseId  from AssetLeases where recordGuid = @EntityId)
                                select DocId  from ShoppingCart_items WHERE Forentityid = @LeaseId ";
            }
            else if (request.EntityType == 3)
            {
                sql = $@"declare @AssetId int
                            declare @CredentialmasterDocId int
                            select @AssetId=AssetId,@CredentialmasterDocId = CredentialmasterDocId  from AssetCredentials where recordGuid = @EntityId
                            
                            select Pd.DocId  from ShoppingCart_items  SCI
                            INNER JOIN Products_Default PD on PD.DocId = SCI.Productid
                            INNER JOIN Products_Links PL on Pl.DocId = PD.DocId
                            INNER JOIN Credentialmaster_Default CMD on CMD.DocId = PL.Entityid
                            WHERE Forentityid = @AssetId and CMD.DocId = @CredentialmasterDocId ";
            }
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EntityId", request.LeaseId, dbType: DbType.Guid);
            //queryParameters.Add("@ProductId", productId, dbType: DbType.Int32);
            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            if (result.Count > 0)
                return false;
            else return true;
        }
    }
}