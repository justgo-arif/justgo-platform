using Dapper;
using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.GetAssetPurchaseDiscountConfig
{
    public class GetAssetPurchaseDiscountConfigHandler : IRequestHandler<GetAssetPurchaseDiscountConfigQuery, AssetDiscountSchemeDTO?>
    {
        private readonly IReadRepositoryFactory _readDb;

        public GetAssetPurchaseDiscountConfigHandler(IReadRepositoryFactory readDb)
        {
            _readDb = readDb;
        }

        public async Task<AssetDiscountSchemeDTO?> Handle(GetAssetPurchaseDiscountConfigQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select TOP 1 RuleConfig from AssetDiscountSchemes where OwnerId = @OwnerId and IsActive = 1";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OwnerId", request.OwnerId, dbType: DbType.Int32);

            var result = await _readDb.GetLazyRepository<AssetDiscountSchemeDTO>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");

            return result;
        }
    }
}