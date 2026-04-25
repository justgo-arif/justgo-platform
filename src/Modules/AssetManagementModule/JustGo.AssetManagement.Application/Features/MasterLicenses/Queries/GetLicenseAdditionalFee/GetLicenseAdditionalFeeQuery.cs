using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenseAdditionalFee
{
    public class GetLicenseAdditionalFeeQuery : IRequest<List<AssetSurchargeDTO>>
    {
        public GetLicenseAdditionalFeeQuery(string[] licenseIds)
        {
            LicenseIds = licenseIds;
        }
        public string[] LicenseIds { get; set; }
    }
}
