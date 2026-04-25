using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.DTOs.BookingTransferRequestDTOs
{
  

    public class MemberPlanStatusResultDto
    {
        public bool AllMembersEligible { get; set; }
        public List<MemberPlanStatusDto> MembersWithIssues { get; set; } = new();
    }
}
