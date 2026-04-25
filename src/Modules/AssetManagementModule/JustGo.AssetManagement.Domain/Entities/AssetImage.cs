using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetImages : RecordInfo
    {
        public int AssetImageId { get; set; }
        public int AssetId { get; set; }
        public string AssetImage { get; set; }
        public bool IsPrimary { get; set; } 

    }

}
