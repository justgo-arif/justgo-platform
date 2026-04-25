using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities
{
    public class SystemSettings
    {
        public int ItemId { get; set; }
        public string ItemKey { get; set; }
        public int KeyGroup { get; set; }
        public int ModuleId { get; set; }
        public string Value { get; set; }
        public bool IsPersonalizable { get; set; }
        public bool IsSyncAble { get; set; }
        public bool Restricted { get; set; }    
    }
}
