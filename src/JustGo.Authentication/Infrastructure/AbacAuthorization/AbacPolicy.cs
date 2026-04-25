using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class AbacPolicy
    {
        public int Id { get; set; }
        public string PolicyName { get; set; }
        public string PolicyDescription { get; set; }
        public string PolicyRule { get; set; }
        public int ParentPolicyId { get; set; }
        public string PolicyEntryPoint { get; set; }
    }
}
