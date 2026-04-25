using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Dapper;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using Newtonsoft.Json;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.Authentication.Infrastructure.Logging;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.CancelLicenses
{
    public class CancelLicenseCommandHandler : IRequestHandler<CancelLicenseCommand, bool>
    {
        private readonly IUtilityService _utilityService;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;

        public CancelLicenseCommandHandler(
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(CancelLicenseCommand request, CancellationToken cancellationToken)
        {

            if(request.Reason != LicenseCancelReason.None)
            {
                int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

                var queryParameters = new DynamicParameters();
                queryParameters.Add("@RecordGuid", request.AssetLicenseId);
                var assetLicense = await _readRepository.GetLazyRepository<AssetLicense>().Value
                                     .GetAsync($@"select * from AssetLicenses Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

                assetLicense.CancelReason = request.Reason;
                assetLicense.RecordRemarks = request.Note;
                assetLicense.CancelEffectiveFrom = request.EffectiveFrom;
                assetLicense.SetUpdateInfo(currentUserId);

                var (sql, qparams) = SQLHelper
                    .GenerateUpdateSQLWithParameters(assetLicense, "RecordGuid",
                    new string[] { "AssetLicenseId" },
                    "AssetLicenses");
                await _writeRepository.GetLazyRepository<object>().Value
                    .ExecuteAsync(sql, cancellationToken, qparams, null, "text");

                CustomLog.Event(AuditScheme.AssetManagement.Value,
                    AuditScheme.AssetManagement.AssetLicense.Value,
                    AuditScheme.AssetManagement.AssetLicense.Canceled.Value,
                   currentUserId,
                   assetLicense.AssetLicenseId,
                   LogEntityType.Asset,
                   assetLicense.AssetId,
                   AuditScheme.AssetManagement.AssetLicense.Canceled.Name,
                   "Asset License Canceled;" + JsonConvert.SerializeObject(request)
                  );

                return true;
            }
            else
            {
                throw new CustomValidationException("Must Provide A Reason!");
            }

        }


    }
}
