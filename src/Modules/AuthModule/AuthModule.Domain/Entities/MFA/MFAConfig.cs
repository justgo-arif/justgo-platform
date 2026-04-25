using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    
namespace AuthModule.Domain.Entities.MFA
{
    public class MFAConfig
    {
        public string MFAApiURL { get; set; }
        public string MFAApiKey { get; set; }
    }
}
