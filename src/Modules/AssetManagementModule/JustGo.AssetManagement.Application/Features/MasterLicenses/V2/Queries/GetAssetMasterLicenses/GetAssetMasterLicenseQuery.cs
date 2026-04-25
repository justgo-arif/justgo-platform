using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.AssetManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetAssetMasterLicenses
{
    public class GetAssetMasterLicenseQuery:IRequest<List<DTOs.AssetMasterLicenseDTO>>
    {
     public GetAssetMasterLicenseQuery(int licenseType, Guid assetTypeId,Guid assetRegisterId)
    {
        AssetTypeId = assetTypeId;
        LicenseType = licenseType;
        AssetRegisterId = assetRegisterId;
    }

    public int LicenseType { get; set; }
    public Guid AssetTypeId { get; set; }
    public Guid AssetRegisterId { get; set; }
        //public Guid OwnerId { get; set; }
    
    }
}
