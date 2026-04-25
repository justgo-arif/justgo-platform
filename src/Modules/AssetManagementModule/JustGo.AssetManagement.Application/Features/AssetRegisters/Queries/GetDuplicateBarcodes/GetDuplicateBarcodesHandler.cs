using Dapper;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetDuplicateBarcodes
{

    public partial class GetDuplicateBarcodesHandler : IRequestHandler<GetDuplicateBarcodesQuery, bool>
    {



        private readonly IReadRepositoryFactory _readRepository;


        public GetDuplicateBarcodesHandler(
            IReadRepositoryFactory readRepository
            )
        {


            _readRepository = readRepository;
        }

        public async Task<bool> Handle(GetDuplicateBarcodesQuery request, CancellationToken cancellationToken)
        {

            return await GetDuplicateBarcodes(request, cancellationToken);

        }

        private async Task<bool> GetDuplicateBarcodes(GetDuplicateBarcodesQuery request, CancellationToken cancellationToken)
        {
            var sql = $@"Select Top(1) * from AssetRegisters ar 
                     Where 
                     ar.Barcode = @Barcode and
                     ar.RecordGuid != @RecordGuid";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Barcode", request.Barcode);
            queryParameters.Add("@RecordGuid", 
                string.IsNullOrEmpty(request.AssetRegisterId) ?
                "" : request.AssetRegisterId
                );
            var result = (await _readRepository.GetLazyRepository<AssetRegister>()
                     .Value.GetAsync(sql, cancellationToken, queryParameters,
                         null, "text"));


            return result != null;
        }


    }
}
