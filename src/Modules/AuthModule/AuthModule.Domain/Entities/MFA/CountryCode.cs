using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities.MFA
{
    public class CountryCodes   
    {
        public string CountryName { get; set; }
        public string CountryCode { get; set; }
        public string PhoneCode { get; set; }
        public string URL { get; set; } 
    }
}
