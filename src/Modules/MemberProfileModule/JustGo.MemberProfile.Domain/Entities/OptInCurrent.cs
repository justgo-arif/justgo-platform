using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class OptInCurrent
    {
        public int Id { get; set; }

        public int EntityId { get; set; }

        public int OptInId { get; set; }

        public bool Value { get; set; }

        public int Version { get; set; }

        public int LastModifiedUser { get; set; }

        public DateTime ActionDate { get; set; }
    }
}
