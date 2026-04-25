using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Classes
{
    public class AttendeeDto
    {
        public int AttendeeId { get; set; }
        public string MemberDocId { get; set; }
        public Guid UserSyncId { get; set; }
        public int SessionId { get; set; }
        public int OccurrenceId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductReference { get; set; }
        public int? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? FullName { get; set; }
        public string? DayOfWeek { get; set; }
        public DateTime? StartDate { get; set; }
        public string? ProfilePicURL { get; set; }
        public int? UserId { get; set; }
        public string? Gender { get; set; }
        public DateTime? DOB { get; set; }
        public int? AttendeeStatus { get; set; }
        public string? Status { get; set; }
        public string Note { get; set; }
        public int? AttendeeDetailNoteId { get; set; }
        public int AttendeeDetailsStatus { get; set; }
        public int AttendeeDetailsId { get; set; }
        public int? TimeZoneId { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public string? AttendeeType { get; set; }
        public string SessionName { get; set; }
        public string BookedBy { get; set; }
        public string Reference { get; set; }
        public DateTime? BookingDate { get; set; }
        public int? PaymentId { get; set; }
        public string? MemberId { get; set; }
        public bool? IsPhotoConsent { get; set; }
        public bool? IsAlertNote { get; set; }
        public bool? IsMedicalNote { get; set; }
        public int? RowId { get; set; }
        public int? TotalCount { get; set; }
    }
}
