using JustGo.AssetManagement.Application.DTOs.AssetLeases;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Commands.CreateLeases
{
    public class CreateAssetLeaseCommand:AssetLeaseDTO, IRequest<string>
    {
    }
}
