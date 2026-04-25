using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetStatusMetaData;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetLeaseStatusMetaData
{
    public class GetLeaseStatusMetaDataHandler : IRequestHandler<GetLeaseStatusMetaDataQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<AssetStatus>> _readRepository;

        public GetLeaseStatusMetaDataHandler(LazyService<IReadRepository<AssetStatus>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetLeaseStatusMetaDataQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT * FROM [dbo].[AssetStatus] WHERE Type = 2";
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
