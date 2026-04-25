using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.DeleteLicense
{
    public class DeleteAssetLicenseCommand: IRequest<string>
    {
        public string AssetLicenseId { get; set; }
    }
}
