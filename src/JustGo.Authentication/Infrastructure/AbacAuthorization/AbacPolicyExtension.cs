using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class AbacPolicyExtension
    {
        public int PolicyExtensionId { get; set; }
        public int PolicyId { get; set; }
        public string ResourceKey { get; set; }
        public string ReturnType { get; set; }
        public string SqlQuery { get; set; }
        public string SqlParams { get; set; }
    }
}
