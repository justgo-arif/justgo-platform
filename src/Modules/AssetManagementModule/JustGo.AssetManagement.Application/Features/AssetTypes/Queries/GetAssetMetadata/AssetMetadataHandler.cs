using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata
{
    public class AssetMetadataHandler : IRequestHandler<AssetMetadataQuery, AssetTypeDto>
    {
        private readonly LazyService<IReadRepository<AssetType>> _readRepository;

        public AssetMetadataHandler(LazyService<IReadRepository<AssetType>> readRepository)
        {
            _readRepository = readRepository;

        }

        public async Task<AssetTypeDto> Handle(AssetMetadataQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT * FROM [dbo].[AssetTypes] Where RecordGuid = @AssetTypeId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", request.AssetTypeId, dbType: DbType.Guid);

            var data = (await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text"));
            return new AssetTypeDto()
            {
                TypeId = data.RecordGuid,
                TypeName = data.TypeName,
                AssetRegistrationConfig = JsonConvert.DeserializeObject<AssetRegistrationConfig>(data.AssetRegistrationConfig),
                AssetTypeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(data.AssetTypeConfig),
                AssetLeaseConfig = JsonConvert.DeserializeObject<AssetLeaseConfig>(data.AssetLeaseConfig),
                AssetTransferConfig = JsonConvert.DeserializeObject<AssetTransferConfig>(data.AssetTransferConfig),
            };
        }

    }
}
