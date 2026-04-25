using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.AssetManagement.Application.Features.Common.Helpers;

namespace JustGo.AssetManagement.Application.Features.Common.Queries.CheckTranferPedingByAssetId
{
    public class CheckTranferPedingByAssetIdHandler : IRequestHandler<CheckTranferPedingByAssetIdQuery, bool>
    {
        private readonly IReadRepositoryFactory _readRepository;
        public CheckTranferPedingByAssetIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<bool> Handle(CheckTranferPedingByAssetIdQuery request, CancellationToken cancellationToken)
        {

            string sql = @" SELECT TOP 1 AOT.RecordGuid
                            FROM AssetOwnershipTransfers AOT 
                            INNER JOIN AssetRegisters AR ON AR.AssetId = AOT.AssetId
                            WHERE AR.RecordGuid = @AssetId
                            AND AOT.TransferStatusId IN @PendingStatusIds
                            ";

            var parameters = new
            {
                AssetId = request.AssetRegisterId,
                PendingStatusIds = AssetStatusHelper.getTransferActionStatusIds()
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
