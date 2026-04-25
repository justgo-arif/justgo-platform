using Dapper;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetDuplicateAsset
{

    public partial class GetDuplicateAssetHandler : IRequestHandler<GetDuplicateAssetQuery, AssetRegister>
    {



        private readonly IReadRepositoryFactory _readRepository;


        public GetDuplicateAssetHandler(
            IReadRepositoryFactory readRepository
            )
        {


            _readRepository = readRepository;
        }

        public async Task<AssetRegister> Handle(GetDuplicateAssetQuery request, CancellationToken cancellationToken)
        {

            return await GetDuplicateAsset(request, cancellationToken);

        }

        private async Task<AssetRegister> GetDuplicateAsset(GetDuplicateAssetQuery request, CancellationToken cancellationToken)
        {
            var sql = $@"Select Top(1) * from AssetRegisters ar 
                     Where 
                     ar.AssetName = @AssetName and
                     ar.StatusId != 7  and
                     ar.RecordGuid != @RecordGuid";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetName", request.AssetName);
            queryParameters.Add("@RecordGuid", 
                string.IsNullOrEmpty(request.AssetRegisterId) ?
                "" : request.AssetRegisterId
                );
            var result = (await _readRepository.GetLazyRepository<AssetRegister>()
                     .Value.GetAsync(sql, cancellationToken, queryParameters,
                         null, "text"));


            return result;
        }


    }
}
