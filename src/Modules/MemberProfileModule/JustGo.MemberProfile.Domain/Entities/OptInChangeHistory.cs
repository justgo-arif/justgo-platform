using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class OptInChangeHistory
    {
        public int Id { get; set; }

        public int OptInMasterId { get; set; }

        public string OptInContent { get; set; }

        public int Version { get; set; }

        public int ActionUser { get; set; }

        public string ActionUserName { get; set; }

        public string ActionUserEmailAddress { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string ModifiedTime { get; set; }
        public string SyncGuid { get; set; }
    }
}
