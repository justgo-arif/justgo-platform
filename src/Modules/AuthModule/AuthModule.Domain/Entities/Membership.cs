using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities
{
    public class Membership
    {
        public string SyncGuid { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
    }
}
