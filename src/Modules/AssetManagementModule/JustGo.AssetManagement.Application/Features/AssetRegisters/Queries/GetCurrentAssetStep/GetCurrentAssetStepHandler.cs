using Dapper;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetCurrentAssetStep
{
    public class GetCurrentAssetStepHandler : IRequestHandler<GetCurrentAssetStepQuery, string>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetCurrentAssetStepHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<string> Handle(GetCurrentAssetStepQuery request, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetRegisterId);
            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                 .GetAsync($@"Select * from AssetRegisters Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", asset.AssetId);
            string step = (string)(await _readRepository.GetLazyRepository<string>().Value
                 .GetSingleAsync($@"EXEC GetCurrentStepByAssetId  @AssetId", cancellationToken, queryParameters, null, "text"));

            return step;
        }
    }
}
