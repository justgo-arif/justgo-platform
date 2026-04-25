using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Event
{
    public class BookingDate
    {
        public int RowId { get; set; }
        public string ScheduleDateWithDay { get; set; } 
        public string ScheduleDate { get; set; } 
    }

    public class BookingDateCommand
    {
        public int RowId { get; set; }
        public int DocId { get; set; }
        public string ScheduleDateWithDay { get; set; }
        public string ScheduleDate { get; set; }
    }

    public class BookingDateCommandDto
    {
        public int ScheduleId { get; set; }
        public int EventDocId { get; set; }
        public int EntityTypeId { get; set; }
        public int OwnerId { get; set; }
        public string DayOfWeek { get; set; }
        public DateTime OccurrenceDate { get; set; }
    }
}
