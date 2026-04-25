using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.Credential;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetGuidByCredentialGuid
{
    public class GetAssetGuidByCredentialGuidHandler : IRequestHandler<GetAssetGuidByCredentialGuidQuery, PermissionParam>
    {
        private readonly LazyService<IReadRepository<PermissionParam>> _readRepository;

        public GetAssetGuidByCredentialGuidHandler(LazyService<IReadRepository<PermissionParam>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<PermissionParam> Handle(GetAssetGuidByCredentialGuidQuery request, CancellationToken cancellationToken)
        {
            var sql = @"SELECT ar.RecordGuid FROM AssetRegisters ar
                            INNER JOIN AssetCredentials ac ON ar.AssetId=ac.AssetId
                        WHERE ac.RecordGuid = @CredentialGuid;
                        SELECT d.SyncGuid FROM [dbo].[Document] d
	                        INNER JOIN Credentialmaster_Default cmd ON cmd.DocId=d.DocId
	                        INNER JOIN [dbo].AssetCredentials ac ON cmd.DocId = ac.CredentialmasterDocId
                        WHERE ac.RecordGuid = @CredentialGuid;";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CredentialGuid", request.CredentialGuid);
            await using var multi = await _readRepository.Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");
            var result = new PermissionParam();
            result.AssetRegisterId = (await multi.ReadSingleAsync<string>());
            result.EntityId = (await multi.ReadSingleAsync<string>());
            return result;
        }
    }
}
