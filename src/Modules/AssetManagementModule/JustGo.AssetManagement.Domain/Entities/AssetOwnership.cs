using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{

    public class AssetOwnership
    {
        public int AssetOwnershipId { get; set; }

        public int AssetId { get; set; }

        public int? OwnerId { get; set; }

        public OwnerType OwnerType { get; set; }

        public int? EntityId { get; set; }

        public OwnershipEntityType EntityType { get; set; }
    }
    
}
