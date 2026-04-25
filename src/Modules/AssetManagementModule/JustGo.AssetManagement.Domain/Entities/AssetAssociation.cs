using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetAssociation : RecordInfo
    {
        public int AssetAssociationId { get; set; }
        public int AssetId { get; set; }
        public int TargetEntityId { get; set; }
        public RelationshipType RelationshipType { get; set; } 
    }

}
