using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetLease : RecordInfo
    {
        public int AssetLeaseId { get; set; }
        public int AssetId { get; set; }
        public DateTime LeaseStartDate { get; set; }
        public DateTime LeaseEndDate { get; set; }
        public int StatusId { get; set; }
        public AssetLeaseDateRangeType DateRangeType { get; set; }
        public string Comments { get; set; }
        public RejectionReason RejectionReason { get; set; }
        public int? PaymentId { get; set; } 
        public int ? OwnerClubId { get; set; }

    }

}
