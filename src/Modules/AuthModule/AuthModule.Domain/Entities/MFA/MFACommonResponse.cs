using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities.MFA 
{
    public class MFACommonResponse
    {
        public bool EnableAuthenticatorApp { get; set; }
        public bool EnableWhatsapp { get; set; }
        public string WhatsappNumber { get; set; }
        public string StatusMessage { get; set; }
        public string Email { get; set; }
        public bool IsEmailAuthEnabled { get; set; }
        public bool IsDeviceRemembered { get; set; }
    }
}
