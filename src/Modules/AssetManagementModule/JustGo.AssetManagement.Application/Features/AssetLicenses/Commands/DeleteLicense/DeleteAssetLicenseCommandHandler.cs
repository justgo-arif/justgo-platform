using Dapper;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands;
using Newtonsoft.Json;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Helpers;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.DeleteLicense
{
    public class DeleteAssetLicenseCommandHandler : IRequestHandler<DeleteAssetLicenseCommand, string>
    {

        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUtilityService _utilityService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        public DeleteAssetLicenseCommandHandler(
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IUtilityService utilityService,
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }
        public async Task<string> Handle(DeleteAssetLicenseCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            return await DeleteLicensesByLicenseId(command, currentUserId, cancellationToken);
        }

        public async Task<string> DeleteLicense(DeleteAssetLicenseCommand command, int currentUserId, CancellationToken cancellationToken)
        {
            var qparams = new DynamicParameters();
            qparams.Add("@RecordGuid", command.AssetLicenseId);
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"declare @AssetId INT
                                                                                    Declare @AssetLicenseId INT
                                                                                    select @AssetId = AssetId,@AssetLicenseId = AssetLicenseId from AssetLicenses WHERE RecordGuid=@RecordGuid
                                                                                    exec ResolveReasonsForAssetId @UserId = {currentUserId}, @AssetId = @AssetId, @LeaseId = null, @AssetLicenseId = @AssetLicenseId",
                                                cancellationToken, null, null, "text");
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"DELETE FROM AssetLicenses Where RecordGuid = @RecordGuid ", qparams, null, "text");
            return command.AssetLicenseId;

        }

        public async Task<string> DeleteLicensesByLicenseId(DeleteAssetLicenseCommand command, int currentUserId, CancellationToken cancellationToken)
        {



            var qparams = new DynamicParameters();
            qparams.Add("@AssetLicenseId", command.AssetLicenseId);

            int assetLicenseId = _mediator.Send(new GetIdByGuidQuery()
            {
                Entity = AssetTables.AssetLicenses,
                RecordGuids = new List<string>() { command.AssetLicenseId }
            }).Result[0];

            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                    .GetAsync($@"select * from AssetRegisters Where AssetId = 
                     (select AssetId from AssetLicenses where RecordGuid = @AssetLicenseId)"
                    ,cancellationToken, qparams, null, "text");

            string sql = @"EXEC DELETE_ASSET_LICENSES @RecordGuid =@AssetLicenseId";

            int rowsAffected = await _writeRepository.GetLazyRepository<object>().Value.
                ExecuteAsync( sql, qparams, null, "text");



            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.AssetLicense.Value,
                AuditScheme.AssetManagement.AssetLicense.Deleted.Value,
               currentUserId,
               assetLicenseId,
               LogEntityType.Asset,
               asset.AssetId,
               AuditScheme.AssetManagement.AssetLicense.Deleted.Name,
               "Asset License Deleted;" + JsonConvert.SerializeObject(command)
              );

            if (AssetStatusHelper.checkIsActionStatusId(asset.StatusId))
            {
                await _mediator.Send(new AssetStateAllocationCommand()
                {
                    AssetRegisterId = asset.RecordGuid
                });

            }


            return command.AssetLicenseId;
        }
    }
}
