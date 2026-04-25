using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetCredentials
{
    public class GetAssetCredentialsHandler : IRequestHandler<GetAssetCredentialsQuery, List<AssetCredentialDTO>>
    {
        private readonly LazyService<IReadRepository<AssetCredentialDTO>> _readRepository;

        public GetAssetCredentialsHandler(
            LazyService<IReadRepository<AssetCredentialDTO>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<AssetCredentialDTO>> Handle(GetAssetCredentialsQuery request, CancellationToken cancellationToken)
        {
            string dataSql = $@"select 
                      ac.AssetCredentialId as DocId,
					  ac.RecordGuid as AssetCredentialId,
                      cmd.Credentialname , 
                      ISNULL(cmd.ShortName,'') ShortName, 
                      ISNULL(cmd.CredentialCode,'') CredentialCode, 
                      S.AssetStatusId, 
                      S.name [StateName], 
                      cmd.Credentialcategory as [CredentialCategory],
					  ac.StartDate,
					  ac.EndDate as ExpiryDate,
					  D.SyncGuid  as MasterCredentialId
                      from 
                      AssetCredentials ac 
                      inner join AssetRegisters ar on ar.AssetId = ac.AssetId 
                      inner join credentialmaster_default cmd on cmd.DocId = ac.CredentialmasterDocId
					  inner join AssetStatus S on S.AssetStatusId =  ac.StatusId
					  inner join Document D on D.DocId = cmd.DocId
                      where 
                      ar.RecordGuid = @RecordGuid
					  order by ac.AssetCredentialId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetRegisterId);

            var data = (await _readRepository.Value.GetListAsync(dataSql, cancellationToken, queryParameters, null, "text")).ToList();

            return data;
        }
    }
}
