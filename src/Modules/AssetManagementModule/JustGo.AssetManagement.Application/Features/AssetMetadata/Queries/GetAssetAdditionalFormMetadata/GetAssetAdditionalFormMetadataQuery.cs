using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetAdditionalFormMetadata
{
    public class GetAssetAdditionalFormMetadataQuery : IRequest<List<AssetAdditionalFormMetadata>>
    {
        public string AssetTypeId { get; set; }
    }
}