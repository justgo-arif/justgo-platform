using Dapper;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;

namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetTransferStatuses
{
    public class GetTransferStatusIdHandler : IRequestHandler<GetTransferStatusIdQuery, int>
    {
        private readonly LazyService<IReadRepository<AssetStatus>> _readRepository;

        public GetTransferStatusIdHandler(LazyService<IReadRepository<AssetStatus>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<int> Handle(GetTransferStatusIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT * FROM [dbo].[AssetStatus] WHERE Type = 5 and Name = @Name";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Name", Utilities.GetEnumText(request.Status));
            var assetStatus = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            return assetStatus.AssetStatusId;
        }
    }
}
