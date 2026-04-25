using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenses
{
    public class GetMasterLicenses : IRequest<List<AssetMetaDataMasterLicenseDTO>>
    {
        public GetMasterLicenses(int licenseType,Guid assetTypeId)
        {
            AssetTypeId = assetTypeId;
            LicenseType = licenseType;
        }

        public int LicenseType { get; set; }
        public Guid AssetTypeId { get; set; }
        //public Guid OwnerId { get; set; }
    }
}
