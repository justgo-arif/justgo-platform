using JustGo.AssetManagement.Application.DTOs.Credential;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetCredentialProduct
{
    public class GetAssetCredentialProductsQuery:IRequest<List<AssetCredentialProductDTO>>
    {
        public int CredentialDocId { get; set; }
    }
}
