using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetTypesTag : BaseEntity
    {
        public int TagId { get; set; }
        public string Name { get; set; }
        public int AssetTypeId { get; set; }
    }

}
