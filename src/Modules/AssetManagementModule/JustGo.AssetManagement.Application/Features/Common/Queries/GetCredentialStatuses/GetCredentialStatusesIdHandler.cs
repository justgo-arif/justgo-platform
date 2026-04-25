using Dapper;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetCredentialStatuses
{
    public class GetCredentialStatusIdHandler : IRequestHandler<GetCredentialStatusIdQuery, int>
    {
        private readonly LazyService<IReadRepository<AssetStatus>> _readRepository;

        public GetCredentialStatusIdHandler(LazyService<IReadRepository<AssetStatus>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<int> Handle(GetCredentialStatusIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT * FROM [dbo].[AssetStatus] WHERE Type = 3 and Name = @Name";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Name", Utilities.GetEnumText(request.Status));
            var assetStatus = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            return assetStatus.AssetStatusId;
        }
    }
}
