using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.DTOs.BookingTransferRequestDTOs
{
    public class MemberPlanStatusDto
    {
        //public bool AllMembersEligible { get; set; }
        //public List<MemberPlanStatusDto> MembersWithIssues { get; set; } = new();

        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public bool HasPendingSchedule { get; set; }
        public bool HasTroubleshootSchedule { get; set; }
        public int PlanId { get; set; }
        public string PlanStatus { get; set; } = string.Empty;
    }
}
