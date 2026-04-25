using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities
{
    public class Policy
    {
        public int Id { get; set; }
        public string PolicyName { get; set; }
        public string PolicyDescription { get; set; }
        public string PolicyRule { get; set; }
        public int ParentPolicyId { get; set; }
    }
}
