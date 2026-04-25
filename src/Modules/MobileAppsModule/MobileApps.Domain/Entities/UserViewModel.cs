using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities
{
    public class UserViewModel
    {
        public int Userid { get; set; }
        public string MemberId { get; set; }
        public int MemberDocId { get; set; }
        public string ProfilePicURL { get; set; }
        public string Gender { get; set; }
        public string EmailAddress { get; set; }
        public string Contact { get; set; }
    }
}
