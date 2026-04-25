using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs
{
    // Session Info Response (First Query)
    public class SessionInfoDto
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string? AgeGroupName { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string ColorHexCode { get; set; } = string.Empty;
    }

    // Session Statistics Response (Second Query)
    public class SessionStatisticsDto
    {
        public int Capacity { get; set; }
        public int TotalAttended { get; set; }
        public int TotalTrial { get; set; }
        public int Expected { get; set; }
    }

    // Attendee Response (Main Query)
    public class AttendeeDto
    {
        public int MemberDocId { get; set; }
        public int CourseBookingDocId { get; set; }
        public int TicketDocId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool HasPhotoConsent { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public Guid UserSyncId { get; set; }
        public int AttendeeDetailsStatus { get; set; }
        public int AttendeeType { get; set; }
        public string Status { get; set; } = string.Empty;
        internal string? MemberNotesJson { get; set; }
        internal string? AttendanceNotesJson { get; set; }
        internal string? EmergencyContactJson { get; set; }

        public MemberNotesDto MemberNotes => string.IsNullOrEmpty(MemberNotesJson)
            ? null!
            : JsonSerializer.Deserialize<MemberNotesDto>(MemberNotesJson)!;

        public AttendanceNotesDto AttendanceNotes => string.IsNullOrEmpty(AttendanceNotesJson)
            ? null!
            : JsonSerializer.Deserialize<AttendanceNotesDto>(AttendanceNotesJson)!;

        public List<AttendeeEmergencyContactDto> EmergencyContact => string.IsNullOrEmpty(EmergencyContactJson)
            ? null!
            : JsonSerializer.Deserialize<List<AttendeeEmergencyContactDto>>(EmergencyContactJson)!;

    }

    // Paginated Response
    public class AttendeeListResponseDto
    {
        public List<AttendeeDto> Attendees { get; set; } = new List<AttendeeDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    // Complete Response (All Queries Combined)
    public class SessionAttendanceResponseDto
    {
        public SessionInfoDto SessionInfo { get; set; } = new SessionInfoDto();
        public SessionStatisticsDto Statistics { get; set; } = new SessionStatisticsDto();
        public AttendeeListResponseDto AttendeeList { get; set; } = new AttendeeListResponseDto();
    }

    public class MemberNotesDto
    {
        public int MemberDocId { get; set; }
        public bool HasMedicalNote { get; set; }
        public bool HasAlertNote { get; set; }
        public bool HasMultipleNotes { get; set; }
        public string Details { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }
    public class AttendanceNotesDto
    {
        public int Id { get; set; }
        public int AttendanceId { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
    public class AttendeeEmergencyContactDto
    {
        public int MemberDocId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }
}
