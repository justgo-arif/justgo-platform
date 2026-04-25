using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Collections.Generic;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.AssetAdditionalFeeByType
{
    public class GetAssetAdditionalFeeByTypeQuery : IRequest<List<AssetSurchargeDTO>>
    {
        public GetAssetAdditionalFeeByTypeQuery(EntityType type, string entityId, string ownerId)
        {
            Type = type;
            EntityId = entityId;
            OwnerId = ownerId;
        }

        public EntityType Type { get; set; }
        public string EntityId { get; set; }
        public string OwnerId { get; set; }
    }
}