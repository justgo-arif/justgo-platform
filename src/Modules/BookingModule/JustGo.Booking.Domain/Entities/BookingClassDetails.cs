namespace JustGo.Booking.Domain.Entities;

public class BookingClassDetails
{
    public int SessionId { get; set; }
    public required string SessionName { get; set; }
    public required string SessionGuid { get; set; }
    public int Capacity { get; set; }
    public int ClassId { get; set; }
    public required string ClassName { get; set; }
    public required string ClassGuid { get; set; }
    public required string OwningEntitySyncGuid { get; set; }
    public string? Description { get; set; }
    public string? ClassState { get; set; }
    public DateTime BookingStartDate { get; set; }
    public DateTime BookingEndDate { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int AgeGroupId { get; set; }
    public string? AgeGroupName { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public string? Gender { get; set; }
    public int TrialLimit { get; set; }
    public string? ColorName { get; set; }
    public string? ColorCode { get; set; }
    public string? ClassImages { get; set; }
    public bool IsOneOffAvailable { get; set; }
    public decimal OneOffPrice { get; set; }
    public bool IsMonthlyAvailable { get; set; }
    public bool IsDynamicAvailable { get; set; }
    public bool IsHourlyPricingAvailable { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal HourlyPrice { get; set; }
    public bool IsPaygAvailable { get; set; }
    public decimal PaygPrice { get; set; }
    public bool IsTrialAvailable { get; set; }
    public decimal TrialPrice { get; set; }
    public string? ScheduleInfo { get; set; }
    public required string VenueName { get; set; }
    public string? VenueAddress1 { get; set; }
    public string? VenueAddress2 { get; set; }
    public string? VenueCounty { get; set; }
    public string? VenuePostcode { get; set; }
    public string? VenueRegion { get; set; }
    public string? VenueCountry { get; set; }
    public string? VenueLatlng { get; set; }
    public List<SessionCoach> SessionCoaches { get; set; } = [];
    public List<SessionOccurrence> SessionOccurrences { get; set; } = [];
    public List<HourlyPricingChart> HourlyPricingCharts { get; set; } = [];
    public bool OneOffApplyProRataDiscount { get; set; }
    public decimal OneOffUnitPricePerSession { get; set; }
    public bool MonthlyApplyProRataDiscount { get; set; }
    public decimal MonthlyUnitPricePerSession { get; set; }
}

public class SessionCoach
{
    public required string MemberId { get; set; }
    public required string CoachName { get; set; }
    public string? Role { get; set; }
    public string? ImageUrl { get; set; }
}

public class SessionOccurrence
{
    public int OccurrenceId { get; set; }
    public int ScheduleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsFuture { get; set; }
}

public class HourlyPricingChart
{
    public int Id { get; set; }
    public decimal HoursPerWeek { get; set; }
    public decimal MonthlyRate { get; set; }
}