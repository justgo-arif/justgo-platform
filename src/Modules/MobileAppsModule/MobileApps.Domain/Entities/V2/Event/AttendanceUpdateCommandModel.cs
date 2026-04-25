using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Event
{
    public class AttendanceUpdateCommandModel
    {
        [Required]
        public int OccurrenceId { get; set; }
        [Required]
        public int AttendeeId { get; set; }
        [Required]
        public int AttendeeType { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public int AttendeeDetailsStatus { get; set; }
        // Note fields
        public string? Note { get; set; }
        public DateTime? ModifiedDate { get; set; }= DateTime.UtcNow;
        public required int TimeZoneId { get; set; }

    }
}
