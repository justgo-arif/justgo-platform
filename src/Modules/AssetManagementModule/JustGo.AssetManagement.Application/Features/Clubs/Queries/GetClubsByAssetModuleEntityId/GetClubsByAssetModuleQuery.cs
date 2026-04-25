using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubsByAssetModuleEntityId
{
    public class GetClubsByAssetModuleQuery : IRequest<List<ClubMemberDTO>>
    {

        public GetClubsByAssetModuleQuery(EntityType entityType, string entityId)
        {
            EntityId = entityId;
            EntityType = entityType;
        }
        public EntityType EntityType { get; set; }
        public string EntityId { get; set; }
    }
}