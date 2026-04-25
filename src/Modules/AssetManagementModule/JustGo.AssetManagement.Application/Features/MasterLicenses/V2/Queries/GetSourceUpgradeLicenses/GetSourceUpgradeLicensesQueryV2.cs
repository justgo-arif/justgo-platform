using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetSourceUpgradeLicenses
{
    public class GetSourceUpgradeLicensesQueryV2 : IRequest<List<SourceUpgradeLicenseDTO>>
    {   public GetSourceUpgradeLicensesQueryV2(int licenseType,int licenseDocId)
        {    
            LicenseType = licenseType;
            LicenseDocId = licenseDocId;
        }
        public int LicenseType { get; set; }
        public int LicenseDocId { get; set; }
    }
}
