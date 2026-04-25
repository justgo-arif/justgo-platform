using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Application.DTOs.MFA   
{
    public class UserMFADto
    {
        public bool EnableAuthenticatorApp { get; set; }
        public string EnableAuthenticatorAppDate { get; set; }
        public bool EnableWhatsapp { get; set; }
        public string EnableWhatsappDate { get; set; }
        public bool IsCreatedWhatsapp { get; set; }
        public bool IsCreatedAuthenticatorApp { get; set; }
        public string WhatsAppNumber { get; set; }
        public string PhoneCode { get; set; }
        public string CountryCode { get; set; }
        public bool IsEnabledWhatsApp { get; set; }
        public string AppStartDateTimezoneName { get; set; }
        public string WhatsAppDateTimezoneName { get; set; }
        public string Email { get; set; }
        public bool IsEmailAuthEnabled { get; set; }
        public string EmailAuthEnableDate { get; set; }
        public string EmailAuthEnableDateTimezoneName { get; set; }
        public bool IsCreatedEmailAuth { get; set; }
        public bool IsVisibleEmailMFA { get; set; }
        public bool IsVisibleWhatsappMFA { get; set; }
        public bool IsVisibleAuthenticatorMFA { get; set; }
    }

}
