using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class OptIn
    {
        public int Id { get; set; }

        public int OptInGroupId { get; set; }

        public string Caption { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool ShowInSignup { get; set; }

        public bool PreTicked { get; set; }

        public bool IsDirty { get; set; }

        public OptInStatus Status { get; set; }

        public int Sequence { get; set; }

        public int Version { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public int LastModifiedUser { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OptInCurrent Current { get; set; }
        public string SyncGuid { get; set; }
    }

    public enum OptInStatus
    {
        Active = 1,
        InActive = 2,
        Archive = 3
    }
}
