using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Membership.Domain.Entities
{
    public class Membership
    {
        public string OrganisationName { get; set; }
        public string MembershipName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
