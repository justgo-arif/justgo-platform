using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Event    
{
    public class AttendanceRecord
    {
        public int CourseBookingDocId { get; set; }
        public string AttendanceStatus { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime AttendanceDate { get; set; }
        public int ScheduleTicketRowId { get; set; }
        public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
    }
}
