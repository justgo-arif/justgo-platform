using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetOwner : RecordInfo
    {
        public int AssetOwnerId { get; set; }
        public int AssetId { get; set; }
        public int OwnerId { get; set; }
        public OwnerType OwnerTypeId { get; set; } 

    }

}
