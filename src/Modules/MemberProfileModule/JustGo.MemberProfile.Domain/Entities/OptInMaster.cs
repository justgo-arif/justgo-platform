using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class OptInMaster
    {
        public int Id { get; set; }

        public string OwnerType { get; set; }

        public int OwnerId { get; set; }

        public string TargetEntity { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int Status { get; set; }

        public int Version { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public string LastModifiedTime { get; set; }

        public int LastModifiedUser { get; set; }

        public bool IsDirty { get; set; }

        public List<OptInGroup> Groups { get; set; }

        public List<OptInChangeHistory> ChangeHistorys { get; set; }
        public string SyncGuid { get; set; }
    }
}
