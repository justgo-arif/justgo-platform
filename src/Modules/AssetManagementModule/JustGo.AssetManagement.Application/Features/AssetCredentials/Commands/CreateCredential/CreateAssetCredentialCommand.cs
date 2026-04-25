using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Credential;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Commands.CreateCredential
{
    public class CreateAssetCredentialCommand: AssetCredentialRequestDTO, IRequest<int>
    {
    }
}
