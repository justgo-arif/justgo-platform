using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetCartAssetLicensesQuery
{
    public class GetCartAssetLicensesQuery : IRequest<List<AssetCartLicenseDTO>>
    {
        public GetCartAssetLicensesQuery(Guid assetRegisterId)
        {
            AssetRegisterId = assetRegisterId;
         
        }
        public Guid AssetRegisterId { get; set; }
    }
  
}
