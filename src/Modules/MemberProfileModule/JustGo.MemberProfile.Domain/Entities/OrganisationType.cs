using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class OrganisationType
    {
        public required string OrganisationTypeName { get; set; }
        public int Sequence { get; set; }
        public List<Organisation> Organisations { get; set; } = [];
    }
}
