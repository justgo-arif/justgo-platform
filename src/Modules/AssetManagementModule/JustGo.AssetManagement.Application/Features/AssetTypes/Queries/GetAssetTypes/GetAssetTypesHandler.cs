using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetTypes
{
    public class GetAssetTypesHandler : IRequestHandler<GetAssetTypesQuery, List<AssetTypeMetadataDto>>
    {
        private readonly LazyService<IReadRepository<AssetTypeMetadataDto>> _readRepository;

        public GetAssetTypesHandler(LazyService<IReadRepository<AssetTypeMetadataDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<AssetTypeMetadataDto>> Handle(GetAssetTypesQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT RecordGuid as AssetTypeId ,TypeName as Name FROM [dbo].[AssetTypes]";
            var queryParameters = new DynamicParameters();
            var assetTypes =(await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return assetTypes;
        }
    }
}
