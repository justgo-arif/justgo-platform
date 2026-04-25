using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetAdditionalFormMetadata
{
    public class GetAssetAdditionalFormMetadataHandler : IRequestHandler<GetAssetAdditionalFormMetadataQuery, List<AssetAdditionalFormMetadata>>
    {
        private readonly LazyService<IReadRepository<AssetAdditionalFormMetadata>> _readRepository;
        private readonly IMediator _mediator;

        public GetAssetAdditionalFormMetadataHandler(LazyService<IReadRepository<AssetAdditionalFormMetadata>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<AssetAdditionalFormMetadata>> Handle(GetAssetAdditionalFormMetadataQuery request, CancellationToken cancellationToken)
        {
            int assetTypeId = (await _mediator.Send(new GetIdByGuidQuery() 
            { 
                Entity = AssetTables.AssetTypes, 
                RecordGuids = new List<string>() { request.AssetTypeId } 
            }))[0];

            string sql = @"SELECT FormId ItemId, '' FormName 
                          FROM AssetTypesFormLink 
                          WHERE AssetTypeId = @AssetTypeId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", assetTypeId);

            var data = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return data;
        }
    }
}