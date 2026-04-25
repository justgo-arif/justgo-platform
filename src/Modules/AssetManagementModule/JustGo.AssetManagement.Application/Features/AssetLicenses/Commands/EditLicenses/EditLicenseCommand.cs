using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.EditLicenses
{
    public class EditLicenseCommand : AssetLicenseEditDTO, IRequest<bool>
    {
        public string AssetLicenseId { get; set; }
    }
}
