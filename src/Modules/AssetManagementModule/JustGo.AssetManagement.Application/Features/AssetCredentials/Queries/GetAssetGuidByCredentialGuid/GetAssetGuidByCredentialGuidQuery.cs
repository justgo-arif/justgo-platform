using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using JustGo.AssetManagement.Application.DTOs.Credential;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetGuidByCredentialGuid
{
    public class GetAssetGuidByCredentialGuidQuery : IRequest<PermissionParam>
    {
        public GetAssetGuidByCredentialGuidQuery(string credentialGuid)
        {
            CredentialGuid = credentialGuid;
        }

        public string CredentialGuid { get; set; }
    }
}
