using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Domain.Entities
{
    public class Document
    {
        public int DocId { get; set; }
        public int RepositoryId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime RegisterDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public int Status { get; set; }
        public string Tag { get; set; } = string.Empty;
        public int Version { get; set; }
        public int UserId { get; set; }
        public Guid SyncGuid { get; set; }
    }

}
