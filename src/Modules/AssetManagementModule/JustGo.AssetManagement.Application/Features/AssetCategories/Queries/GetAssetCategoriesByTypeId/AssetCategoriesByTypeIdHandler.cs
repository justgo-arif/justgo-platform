using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetCategories.Queries.GetAssetCategories
{
    public class AssetCategoriesByTypeIdHandler : IRequestHandler<GetAssetCategoriesByTypeIdQuery, List<AssetCategoryDTO>>
    {

        private readonly IMediator _mediator;
        private readonly LazyService<IReadRepository<AssetCategory>> _readRepository;
        public AssetCategoriesByTypeIdHandler(
            LazyService<IReadRepository<AssetCategory>> readRepository, 
            IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<AssetCategoryDTO>> Handle(GetAssetCategoriesByTypeIdQuery request, CancellationToken cancellationToken)
        {
            int AssetTypeId = (await _mediator.Send(new GetIdByGuidQuery() {  Entity = AssetTables.AssetTypes, RecordGuids = new List<string>() { request.AssetTypeId  } }))[0];

            string sql = @"SELECT * FROM [dbo].[AssetCategories] WHERE AssetTypeId = @AssetTypeId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", AssetTypeId);
            var assetCategories = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return assetCategories.Select(r => 
                new AssetCategoryDTO()
                {
                    CategoryId = r.RecordGuid,
                    Name = r.Name,
                }
            ).ToList();
        }
    }
}
