using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Members
{
    public class MemberDetails
    {
        public PersonalInfo PersonalInfo { get; set; }
        public List<EmergencyContact> EmergencyContacts { get; set; }
    }
   
}
