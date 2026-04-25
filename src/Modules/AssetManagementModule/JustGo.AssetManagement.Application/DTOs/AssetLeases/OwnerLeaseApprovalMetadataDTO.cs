using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetLeases
{
    public class OwnerLeaseApprovalMetadataDTO
    {
        public int StepId { get; set; }

        public int UserDocId { get; set; }
        public string UserId { get; set; }

        public string Fullname { get; set; }
        public string EmailAddress { get; set; }
        public string ProfilePicURL { get; set; }

        public int MemberDocId { get; set; }
        public string MemberId { get; set; }

        public int? ActionStatus { get; set; }
        public string Remarks { get; set; }
    }
}
