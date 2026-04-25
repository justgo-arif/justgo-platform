using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetStatus : BaseEntity
    {
        public int AssetStatusId { get; set; }
        public string Name { get; set; }
        public EntityType Type { get; set; }
    }

}
