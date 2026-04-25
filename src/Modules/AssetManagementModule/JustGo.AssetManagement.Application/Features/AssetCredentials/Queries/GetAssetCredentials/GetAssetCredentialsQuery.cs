using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetCredentials
{
    public class GetAssetCredentialsQuery : IRequest<List<AssetCredentialDTO>>
    {
        public string AssetRegisterId { get; set; }
    }
}
