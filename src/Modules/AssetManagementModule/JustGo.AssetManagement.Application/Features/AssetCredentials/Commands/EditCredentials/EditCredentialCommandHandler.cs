using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Dapper;
using Newtonsoft.Json;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.Authentication.Infrastructure.Logging;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Commands.EditCredentials
{
    public class EditCredentialCommandHandler : IRequestHandler<EditCredentialCommand, bool>
    {
        private readonly IUtilityService _utilityService;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;

        public EditCredentialCommandHandler(
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(EditCredentialCommand request, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetCredentialId);
            var assetCredential = await _readRepository.GetLazyRepository<AssetCredential>().Value
                                 .GetAsync($@"select * from AssetCredentials Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            assetCredential.StartDate = request.StartDate;
            assetCredential.EndDate = request.EndDate;

            assetCredential.SetUpdateInfo(currentUserId);
            var (sql, qparams) = SQLHelper
                .GenerateUpdateSQLWithParameters(assetCredential, "RecordGuid",
                new string[] { "AssetCredentialId" },
                "AssetCredentials");


            await _writeRepository.GetLazyRepository<object>().Value
                .ExecuteAsync(sql, cancellationToken, qparams, null, "text");

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@" update md set md.Granteddate = ac.StartDate,md.Expirydate = ac.EndDate from MembersCredentials_Default_Original  md inner join Assetcredentials ac on ac.CredentialDocId = md.DocId where AssetCredentialId = {assetCredential.AssetCredentialId}",
                                                                          cancellationToken, queryParameters, null, "text");


            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.AssetCredential.Value,
                AuditScheme.AssetManagement.AssetCredential.Updated.Value,
               currentUserId,
               assetCredential.AssetCredentialId,
               LogEntityType.Asset,
               assetCredential.AssetId,
               AuditScheme.AssetManagement.AssetCredential.Updated.Name,
               "Asset Credential Updated;" + JsonConvert.SerializeObject(request)
              );

            return true;
        }


    }
}
