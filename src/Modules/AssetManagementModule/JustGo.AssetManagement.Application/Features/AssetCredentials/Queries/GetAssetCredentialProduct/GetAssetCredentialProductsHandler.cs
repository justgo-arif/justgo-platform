using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Credential;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetCredentialProduct
{
    public class GetAssetCredentialProductsHandler : IRequestHandler<GetAssetCredentialProductsQuery, List<AssetCredentialProductDTO>>
    {
        private readonly LazyService<IReadRepository<AssetCredentialProductDTO>> _readRepository;

        public GetAssetCredentialProductsHandler(
            LazyService<IReadRepository<AssetCredentialProductDTO>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<AssetCredentialProductDTO>> Handle(GetAssetCredentialProductsQuery request, CancellationToken cancellationToken)
        {
            string dataSql = @"select pd.DocId as ProductDocId, pd.Name, pd.Unitprice as price from Credentialmaster_Default CMD inner join
                                Credentialmaster_Links CML on CML.DocId = CMD.DocId
                                inner join Products_Default PD on PD.DocId = CML.Entityid where CMD.DocId = @DocId ";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@DocId", request.CredentialDocId);

            var data = (await _readRepository.Value.GetListAsync(dataSql, cancellationToken, queryParameters, null, "text")).ToList();

            return data;

        }
    }
}
