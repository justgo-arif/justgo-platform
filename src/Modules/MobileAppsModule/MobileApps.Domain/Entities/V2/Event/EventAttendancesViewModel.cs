using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Event
{
    public class EventAttendancesViewModel
    {
        public int CourseBookingDocId { get; set; }
        public int ScheduleTicketRowId { get; set; }
        public string AttandanceStatus { get; set; }
        public DateTime AttandanceDate { get; set; }
        public string Note { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }
}
