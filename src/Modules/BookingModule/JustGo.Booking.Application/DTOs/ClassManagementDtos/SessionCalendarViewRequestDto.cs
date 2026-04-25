using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.DTOs.ClassManagementDtos
{
    public class SessionCalendarViewRequest
    {
        public required Guid SessionGuid { get; set; } 
        public required Guid OwnerGuid { get; set; } 
        public string OccurrenceIds { get; set; } = string.Empty;
        public int RowsPerPage { get; set; } 
        public int PageNumber { get; set; }
        public bool IsActiveMemberOnly { get; set; } = false;
        public string? FilterValue { get; set; } = null;
    }
}
