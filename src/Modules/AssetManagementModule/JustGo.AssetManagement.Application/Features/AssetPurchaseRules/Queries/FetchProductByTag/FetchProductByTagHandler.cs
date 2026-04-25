using Dapper;
using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.FetchProductByTag
{
    public class FetchProductByTagHandler : IRequestHandler<FetchProductByTagQuery, AssetSurchargeDTOV2>
    {
        private readonly LazyService<IReadRepository<AssetSurchargeDTOV2>> _readDb;
        public FetchProductByTagHandler(LazyService<IReadRepository<AssetSurchargeDTOV2>> readDb)
        {
            _readDb = readDb;
        }

        public async Task<AssetSurchargeDTOV2> Handle(
            FetchProductByTagQuery request,
            CancellationToken cancellationToken)
        {
            const string sql = @"select TOP 1 PD.DocId as ProductDocId,D.SyncGuid as ProductId,Pd.Name,'' DisplayName,0 as Price,Producttag Type,ISNULL(PD.Ownerid,0) as OwnerId from Products_Default PD
                                  inner join Document D on D.DocId = PD.Docid where ownerid = @OwnerId and Producttag = @ProductTag ";

            var parameters = new DynamicParameters();
            parameters.Add("@ProductTag", request.ProductTag, DbType.String);
            parameters.Add("@OwnerId", request.OwnerId, DbType.Int32);

            var result = await _readDb
                .Value
                .GetAsync(
                    sql,
                    cancellationToken,
                    parameters,
                    null,
                    "text");

            return result;
        }
    }
}