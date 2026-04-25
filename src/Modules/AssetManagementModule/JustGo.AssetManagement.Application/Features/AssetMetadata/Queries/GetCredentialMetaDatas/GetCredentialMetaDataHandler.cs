using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetCredentialMetaDatas
{
    public class GetCredentialMetaDataHandler : IRequestHandler<GetCredentialMetaDataQuery, List<CredentialItemMetadata>>
    {


        private readonly LazyService<IReadRepository<CredentialItemMetadata>> _readRepository;
        private readonly IMediator _mediator;

        public GetCredentialMetaDataHandler(LazyService<IReadRepository<CredentialItemMetadata>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<CredentialItemMetadata>> Handle(GetCredentialMetaDataQuery request, CancellationToken cancellationToken)
        {

            int AssetTypeId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetTypes, RecordGuids = new List<string>() { request.AssetTypeId } }))[0];

            string sql = @"SELECT  cmd.DocId,cmd.Credentialcode,cmd.Credentialname,ISNULL(cmd.ShortName,'') ShortName ,ISNULL(cmd.Credentialcategory,'') Credentialcategory,ISNULL(cmd.EnableCredentialJourney,0) EnableCredentialJourney,isnull(cmd.Enablecredentialpayment,0)Enablecredentialpayment, d.SyncGuid SyncId,ISNULL(cmd.Aboutcredential,'') as CredentialDescription
                            FROM AssetTypesCredentialsLink atcl
                            INNER JOIN Credentialmaster_Default cmd on cmd.DocId = atcl.CredentialsDocid
                            INNER JOIN Document d on  d.DocId =  cmd.DocId
                            Where atcl.AssetTypeId = @AssetTypeId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", AssetTypeId);
            var data =(await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return data;
        }
    }
}
