using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.Common
{
    public class MapItemDTO<k,v>
    {
        public k Key { get; set; }
        public v Value { get; set; }
    }
}
