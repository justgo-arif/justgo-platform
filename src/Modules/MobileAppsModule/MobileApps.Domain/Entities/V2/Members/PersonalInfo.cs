using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Members
{
    public class PersonalInfo
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
        public string? ProfilePicURL { get; set; }
        public string? Gender { get; set; }
        public string? EmailAddress { get; set; }
        public string? Contact { get; set; }
    }
}
