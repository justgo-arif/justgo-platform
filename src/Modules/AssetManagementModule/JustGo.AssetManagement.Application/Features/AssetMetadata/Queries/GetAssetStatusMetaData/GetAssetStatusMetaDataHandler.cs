using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetStatusMetaData
{
    public class GetAssetStatusMetaDataHandler : IRequestHandler<GetAssetStatusMetaDataQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<AssetStatus>> _readRepository;

        public GetAssetStatusMetaDataHandler(LazyService<IReadRepository<AssetStatus>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetAssetStatusMetaDataQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT * FROM [dbo].[AssetStatus] WHERE Type = 1";
            var queryParameters = new DynamicParameters();
            var statuses =(await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return statuses.Select(r => new SelectListItemDTO<string>()
            {
                Text = r.Name,
                Value = r.RecordGuid
            }).ToList();
        }
    }
}
