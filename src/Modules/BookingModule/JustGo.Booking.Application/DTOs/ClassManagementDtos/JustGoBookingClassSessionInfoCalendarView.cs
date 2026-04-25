using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGoAPI.Shared.Helper.Enums;

namespace JustGo.Booking.Application.DTOs.ClassManagementDtos
{
    public class JustGoBookingClassSessionInfoCalendarView
    {
        public int SessionId { get; set; }

        public string SessionName { get; set; }

        public string ClassSessionGuid { get; set; }

        public string SessionReference { get; set; }

        public int ClassId { get; set; }

        public string ClassName { get; set; }

        public string ClassGuid { get; set; }

        public string ClassReference { get; set; }
        public string AgeGroupName { get; set; }
        public string ColorName { get; set; }
        public string ColorHexCode { get; set; }
        public bool EnableHourlyRate { get; set; }

        public List<JustGoBookingClassSessionPriceOption> SessionPriceOptions { get; set; } =
            new List<JustGoBookingClassSessionPriceOption>();

        public List<JustGoBookingContactDto> JustGoBookingContacts { get; set; } = new List<JustGoBookingContactDto>();

        public List<JustGoBookingClassSessionSchedule> SessionSchedules { get; set; } =
            new List<JustGoBookingClassSessionSchedule>();

        public JustGoBookingClassSessionOption JustGoBookingClassSessionOption { get; set; } =
            new JustGoBookingClassSessionOption();

        public List<DataCaptureItemCreateDTO> DataCaptureItems { get; set; } = new List<DataCaptureItemCreateDTO>();
    }

    public class JustGoBookingClassSessionPriceOption
    {
        public int SessionPriceOptionId { get; set; } // Primary Key
        public int SessionId { get; set; }
        public int SessionOptionId { get; set; }
        public int PriceOption { get; set; } = 1; // Default value
        public decimal Price { get; set; } = 0; // Default value
        public bool ApplyProRataDiscount { get; set; } = false; // Default value
        public decimal UnitPricePerSession { get; set; } = 0; // Default value
        public int IsDynamicPrice { get; set; } = 0; // Default value, assuming 0 means not dynamic
        public bool IsEnable { get; set; } = false; // Default value
    }

    public class JustGoBookingClassSessionSchedule
    {
        public int SessionScheduleId { get; set; }
        public int SessionId { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class JustGoBookingContactDto
    {
        public int BookingContactId { get; set; }
        public int MemberDocId { get; set; }
        public string MemberId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool Published { get; set; }
        public int EntityId { get; set; }
        public int EntityTypeId { get; set; }
        public string ProfilePicURL { get; set; }
        public int UserId { get; set; }

    }

    public class JustGoBookingClassSessionOption
    {

        public int SessionOptionId { get; set; }

        public int SessionId { get; set; }

        // public int PriceOption { get; set; } = 1; // Default value is 1 (Pay Now)

        // public decimal Price { get; set; }

        public int SubscriptionType { get; set; } // Nullable to allow for cases where it might not be applicable

        public int MinAge { get; set; } // Nullable to allow for cases where it might not be applicable

        public int MaxAge { get; set; } // Nullable to allow for cases where it might not be applicable

        public string Gender { get; set; } // Nullable to allow for cases where it might not be applicable

        public bool IsTrial { get; set; } = false; // Default value is false

        public decimal? TrialPrice { get; set; } // Nullable to allow for cases where it might not be applicable

        public int TrialQuantity { get; set; }

        public string MembershipIds { get; set; }

        public bool IsAnyMembershipValid { get; set; } = false; // Default value is false
        public string AdvanceAgeConfig { get; set; }


        public int TrialType { get; set; } = 0; //'FREE' OR 'PAID'  1,2


        public int TrialBookingPeriod { get; set; } = 0;

        public int TrialExpiryPeriod { get; set; } = 0;
        public int SubscriptionRecurringType { get; set; } = 0;
        public int SubscriptionDayOfMonth { get; set; } = 0;

        public string EligibilitySettings { get; set; } = null;
        public string MembershipConfig { get; set; } = "[]";



        // public bool ApplyProRataDiscount { get; set; }= false;


        public int TrialLimit { get; set; }




        // public bool IsPayAsYouGoEnabled { get; set; } = false;
        // public decimal PayAsYouGoFee { get; set; } = 0;



        public List<JustGoBookingClassSessionPriceOption> SessionPriceOptionList { get; set; } =
            new List<JustGoBookingClassSessionPriceOption>();


    }

    public class JustGoBookingSessionEligibilitySettings
    {
        public EligibilitySettings Age { get; set; } = 0;
        public EligibilitySettings Gender { get; set; } = 0;
        public EligibilitySettings Membership { get; set; } = 0;

    }

    public class DataCaptureItemCreateDTO
    {
        //public int ProductId { get; set; }
        public int Id { get; set; }
        public decimal Sequence { get; set; }
        public string Type { get; set; }
        public string Config { get; set; }
    }

    public class DataCaptureItemSelectDTO
    {
        public int ProductId { get; set; }
        public int Id { get; set; }
        public int IsTrail { get; set; }
        public int ProductType { get; set; }
        public decimal Sequence { get; set; }
        public string Type { get; set; }
        public string Config { get; set; }
        public int SessionId { get; set; }
    }

    public class AttendeeCalendarDto
    {
        public int Coursebookingdocid { get; set; }
        public string TicketDocId { get; set; }
        public int RowId { get; set; }
        public int Entityid { get; set; }
        public string UserSyncId { get; set; }
        public string MemberName { get; set; }
        public string ImageUrl { get; set; }
        public bool HasPhotoConsent { get; set; }
        public string ParentFirstname { get; set; }
        public string ParentLastname { get; set; }
        public string ParentEmailAddress { get; set; }
        public string Age { get; set; }
        public bool HasTrial { get; set; }
        public string MemberId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string ChipStatus { get; set; }
        public bool IsTransfer { get; set; }
        public bool IsCancelled { get; set; }
        public List<MemberEmergencyContact> EmergencyContacts { get; set; } = new List<MemberEmergencyContact>();
        public List<AttendeeScheduleDto> AttendeeStatusList { get; set; } = new List<AttendeeScheduleDto>();
        public MemberNotesDto MemberNotes { get; set; } = new MemberNotesDto();
    }

    public class Attendee
    {
        public int RowId { get; set; }
        public int Coursebookingdocid { get; set; }
        public int Entityid { get; set; }
        public string EntityReference { get; set; }
        public string TicketDocId { get; set; }
        public string MemberName { get; set; }
        public string ImageUrl { get; set; }
        public string DOB { get; set; }
        public string Age { get; set; }
        public string EmailAddress { get; set; }
        public string ParentFirstname { get; set; }
        public string ParentLastname { get; set; }

        public string ParentEmailAddress { get; set; }
        public string MemberId { get; set; }
        public List<AttendeeStatus> AttendeeStatusList { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string UserSyncId { get; set; }

        //add HasPhotoConsent bool
        public bool HasPhotoConsent { get; set; }


    }

    public class AttendeeStatus
    {
        public int Id { get; set; } // bigint
        public int CourseBookingDocId { get; set; } // int
        public int ScheduleTicketRowId { get; set; } // int
        public string AttendanceStatus { get; set; } // nvarchar
        public DateTime AttandanceDate { get; set; } // datetime
        public string Note { get; set; } // nvarchar
        public int AttendeeType { get; set; }
        public List<AttendanceNote> AttendanceNotes { get; set; } = new List<AttendanceNote>();


    }

    public class AttendanceNote
    {
        public int Id { get; set; }

        //AttendeeDetailNoteId
        public int AttendanceId { get; set; }
        public string Note { get; set; }
        public string CreatedDate { get; set; }
        public string? ModifiedDate { get; set; }
    }

    public class ListofDate
    {
        public int RowId { get; set; }
        public int TicketDocId { get; set; }
        public string ScheduleDateWithDay { get; set; }
        public DateTime ScheduleDate { get; set; }
    }

    public class AttendeeStatusDto
    {
        public int CourseBookingid { get; set; }
        public int ScheduleTicketRowId { get; set; } // OccurrenceId  
        public string Date { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; }
        public string AttendanceDate { get; set; }
        public string IsTrial { get; set; }
        public int AttendeeDetailsStatus { get; set; }
        public int AttendeeType { get; set; }
        public string CancellationOrChangeLog { get; set; }
    }

    public class AttendeeScheduleDto
    {
        public int Id { get; set; }
        public int Coursebookingdocid { get; set; }
        public int ScheduleTicketRowId { get; set; }
        public string date { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? AttandanceDate { get; set; }
        public string status { get; set; }

        public bool IsTrial { get; set; }

        //public string Note { get; set; }
        public List<AttendanceNote> attendanceNotes { get; set; } = new List<AttendanceNote>();
        public int AttendeeDetailsStatus { get; set; }
        public int AttendeeType { get; set; }
        public CancellationOrChangeLogDto CancellationOrChangeLog { get; set; }
    }

    public class CancellationOrChangeLogDto
    {
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ActionType { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string TimeZoneName { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; } = new DateTime(1, 1, 1, 0, 0, 0);
        public string ActionTime { get; set; } = string.Empty;
    }

    public class AttendeeChipStatusDto
    {
        public int PaymentReferenceId { get; set; }
        public int CourseBookingId { get; set; }
        public string ChipStatus { get; set; }
        public bool IsTransfer { get; set; }
        public bool IsCancelled { get; set; }
    }

    public class JustGoBookingScheduleOccurrence
    {
        public int OccurrenceId { get; set; }
        public int ScheduleId { get; set; }
        public int EntityTypeId { get; set; }
        public int OwnerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AdditionalHolidayId { get; set; }
        public string HolidayName { get; set; }
    }

    public class MemberEmergencyContact
    {
        public int MemberDocId { get; set; }
        public string Name { get; set; }
        public string Relation { get; set; }
        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }
    }

    public class ScheduleOccurrenceDto
    {
        public int RowId { get; set; }
        public int TicketDocId { get; set; }
        public string ScheduleDateWithDay { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public bool IsHoliday { get; set; }
        public string HolidayName { get; set; }
    }

    public class CalendarViewResponseDto
    {
        public JustGoBookingClassSessionInfoCalendarView? SessionInfo { get; set; }
        public List<AttendeeCalendarDto> Attendees { get; set; } = new List<AttendeeCalendarDto>();
        public List<ScheduleOccurrenceDto> ListOfDates { get; set; } = new List<ScheduleOccurrenceDto>();
        public int TotalCount { get; set; }

    }
}
