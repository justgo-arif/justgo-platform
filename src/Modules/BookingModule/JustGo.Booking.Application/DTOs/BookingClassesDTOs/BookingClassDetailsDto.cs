namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs;

public class BookingClassDetailsDto
{
    public required BookingSessionDto Class { get; set; }
    public required SessionVenueDto Venue { get; set; }
    public List<SessionCoachDto>? Coaches { get; set; }
    public required List<SessionOccurrenceDto> Occurrences { get; set; }
    public required List<ScheduleInfoDto> ScheduleInfo { get; set; }
    public List<HourlyPricingChartDto> HourlyPricingChartDto { get; set; } = [];
}

public class BookingSessionDto
{
    public int SessionId { get; set; }
    public required string SessionName { get; set; }
    public Guid SessionGuid { get; set; }
    public int Capacity { get; set; }
    public int ClassId { get; set; }
    public required string ClassName { get; set; }
    public Guid ClassGuid { get; set; }
    public Guid OwningEntitySyncGuid { get; set; }
    public string? ClassState { get; set; }
    public string? Description { get; set; }
    public DateTime BookingStartDate { get; set; }
    public DateTime BookingEndDate { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int AgeGroupId { get; set; }
    public string? AgeGroupName { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public string[]? Gender { get; set; }
    public int TrialLimit { get; set; }
    public string? ColorName { get; set; }
    public string? ColorCode { get; set; }
    public string[]? ClassImages { get; set; }
    public bool IsOneOffAvailable { get; set; }
    public decimal? OneOffPrice { get; set; }
    public bool IsMonthlyAvailable { get; set; }
    public bool IsDynamicAvailable { get; set; }
    public bool IsHourlyPricingAvailable { get; set; }
    public decimal? MonthlyPrice { get; set; }
    public decimal HourlyPrice { get; set; }
    public bool IsPaygAvailable { get; set; }
    public decimal? PaygPrice { get; set; }
    public bool IsTrialAvailable { get; set; }
    public decimal? TrialPrice { get; set; }
    public bool IsWaitable { get; set; }
}

public class SessionVenueDto
{
    public required string Name { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? County { get; set; }
    public string? Postcode { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? Latlng { get; set; }
}

public class SessionCoachDto
{
    public required string MemberId { get; set; }
    public required string CoachName { get; set; }
    public string? Role { get; set; }
    public string? ImageUrl { get; set; }
}

public class SessionOccurrenceDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsHoliday { get; set; }
    public int StartDay => StartDate.Day;
    public string StartDayInString => StartDate.ToString("ddd").ToUpper();
    public string StartTime => StartDate.ToString("hh:mm tt");
    public string EndTime => EndDate.ToString("h:mm tt");
}

public class HourlyPricingChartDto
{
    public int Id { get; set; }
    public decimal HoursPerWeek { get; set; }
    public decimal MonthlyRate { get; set; }
}