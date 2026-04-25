using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.AssetManagement.Application.Features.Common.Helpers;

namespace JustGo.AssetManagement.Application.Features.Common.Queries.CheckLeasePedingByAssetId
{
    public class CheckLeasePedingByAssetIdHandler : IRequestHandler<CheckLeasePedingByAssetIdQuery, bool>
    {
        private readonly IReadRepositoryFactory _readRepository;
        public CheckLeasePedingByAssetIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<bool> Handle(CheckLeasePedingByAssetIdQuery request, CancellationToken cancellationToken)
        {

            string sql = @"SELECT TOP 1 AL.RecordGuid
                            FROM AssetLeases AL INNER JOIN AssetRegisters AR on AR.AssetId = AL.AssetId
                            WHERE AR.RecordGuid = @AssetId
                            AND AL.StatusId IN @PendingStatusIds
                            ";

            var parameters = new
            {
                AssetId = request.AssetRegisterId,
                PendingStatusIds = AssetStatusHelper.getLeaseActionStatusIds()
            };


            var result = await _readRepository.GetLazyRepository<InsertedDataGuidDTO>().Value.GetSingleAsync(sql, cancellationToken, parameters, null, "text");
            if (result == null)
            {
                return false;
            }
            return true;

        }
    }
}
