using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetAuditItemDTO
    {
        public string UserSyncId { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string ProfilePicURL { get; set;  }
        public string MemberDocId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }


    }
}
