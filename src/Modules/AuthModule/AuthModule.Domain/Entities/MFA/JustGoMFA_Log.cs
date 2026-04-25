using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AuthModule.Domain.Entities.MFA 
{
    public class JustGoMFA_Log  
    {
        public int UserId { get; set; }
        public string Type { get; set; } = default;
        public string Action { get; set; } = default;
        public dynamic ParametersJson { get; set; } = default;
        public dynamic Details { get; set; } = default;
        public DateTime Date { get; set; } = default;

    }
}
