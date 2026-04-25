using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetUpgradeLicenses
{
    public class GetUpgradeLicenseQuery: IRequest<List<AssetMasterLicenseDTO>>
    {
        public GetUpgradeLicenseQuery(int licenseType, Guid assetTypeId, Guid licenseId, Guid assetRegisterId)
        {
            AssetTypeId = assetTypeId;
            LicenseType = licenseType;
            LicenseId = licenseId;
            AssetRegisterId = assetRegisterId;
        }

        public int LicenseType { get; set; }
        public Guid AssetTypeId { get; set; }
        public Guid LicenseId { get; set; }
        public Guid AssetRegisterId { get; set; }
    }
}
