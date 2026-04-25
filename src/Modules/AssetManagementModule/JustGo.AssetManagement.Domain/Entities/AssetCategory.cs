using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetCategory : BaseEntity
    {
        public int AssetCategoryId { get; set; }
        public int AssetTypeId { get; set; }
        public string Name { get; set; }
    }
}
