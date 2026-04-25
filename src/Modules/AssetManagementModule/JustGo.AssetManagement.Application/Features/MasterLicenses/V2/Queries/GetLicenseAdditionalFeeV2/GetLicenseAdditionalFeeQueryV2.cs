using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetLicenseAdditionalFeeV2
{
    public class GetLicenseAdditionalFeeQueryV2 : IRequest<List<AssetSurchargeDTOV2>>
    {
        public Guid AssetRegisterId { get; set; }
        public string[] LicenseIds { get; set; }
    }
}
