using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities
{
    public class GlobalSettings
    {
        public string ItemKey { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }    
    }
}
