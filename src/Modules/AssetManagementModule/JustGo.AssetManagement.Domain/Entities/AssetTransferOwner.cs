using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetTransferOwner : RecordInfo
    {
        public int AssetTransferOwnerId { get; set; }
        public int AssetOwnershipTransferId { get; set; }
        public int OwnerId { get; set; }
        public OwnerType OwnerType { get; set; } 

    }

}
