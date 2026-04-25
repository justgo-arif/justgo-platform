using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.JwtAuthentication
{
    public class Membership
    {
        public string SyncGuid { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
    }
}
