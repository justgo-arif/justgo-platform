using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata
{
    public class AssetMetadataQuery : IRequest<AssetTypeDto>
    {
        public AssetMetadataQuery(Guid assetTypeId)
        {
            AssetTypeId = assetTypeId;
        }

        public Guid AssetTypeId { get; set; }
    }
}
