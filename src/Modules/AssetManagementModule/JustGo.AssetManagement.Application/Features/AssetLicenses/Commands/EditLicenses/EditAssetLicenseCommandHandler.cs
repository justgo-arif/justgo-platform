using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Dapper;
using Newtonsoft.Json;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.Authentication.Infrastructure.Logging;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.EditLicenses
{
    public class EditAssetLicenseCommandHandler : IRequestHandler<EditLicenseCommand, bool>
    {
        private readonly IUtilityService _utilityService;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;

        public EditAssetLicenseCommandHandler(
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(EditLicenseCommand request, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetLicenseId);
            var assetLicense = await _readRepository.GetLazyRepository<AssetLicense>().Value
                                 .GetAsync($@"select * from AssetLicenses Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            assetLicense.StartDate = request.StartDate;
            assetLicense.EndDate = request.EndDate;

            assetLicense.SetUpdateInfo(currentUserId);
            var (sql, qparams) = SQLHelper
                .GenerateUpdateSQLWithParameters(assetLicense, "RecordGuid",
                new string[] { "AssetLicenseId" },
                "AssetLicenses");


            await _writeRepository.GetLazyRepository<object>().Value
                .ExecuteAsync(sql, cancellationToken, qparams, null, "text");

            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.AssetLicense.Value,
                AuditScheme.AssetManagement.AssetLicense.Updated.Value,
               currentUserId,
               assetLicense.AssetLicenseId,
               LogEntityType.Asset,
               assetLicense.AssetId,
               AuditScheme.AssetManagement.AssetLicense.Updated.Name,
               "Asset License Updated;" + JsonConvert.SerializeObject(request)
              );

            return true;
        }


    }
}
