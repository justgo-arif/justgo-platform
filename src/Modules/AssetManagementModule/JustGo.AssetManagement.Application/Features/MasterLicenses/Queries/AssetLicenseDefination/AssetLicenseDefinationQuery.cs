using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.AssetManagement.Application.DTOs;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.AssetLicenseDefination
{
    public class AssetLicenseDefinationQuery : IRequest<List<AssetMasterLicenseDTO>>
    {
        public Guid AssetTypeId { get; set; }
        public AssetLicenseDefinationQuery(Guid assetTypeId)
        {
            AssetTypeId = assetTypeId;
        }
    }
}