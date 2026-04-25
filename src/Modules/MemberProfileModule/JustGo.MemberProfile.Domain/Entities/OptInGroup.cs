using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class OptInGroup
    {
        public int Id { get; set; }

        public int OptInMasterId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Sequence { get; set; }

        public List<OptIn> OptIns { get; set; }
        public string SyncGuid { get; set; }
    }
}
