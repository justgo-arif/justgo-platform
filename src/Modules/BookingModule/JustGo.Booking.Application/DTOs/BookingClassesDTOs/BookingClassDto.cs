namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs;

public class BookingClassDto
{
    public required string SessionName { get; set; }
    public required string SessionGuid { get; set; }
    public int Capacity { get; set; }
    public required string ClassName { get; set; }
    public required string ClassGuid { get; set; }
    public required string CategoryName { get; set; }
    public string? AgeGroupName { get; set; }
    public string? OwningEntitySyncGuid { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public string[]? Gender { get; set; }
    public string? ColorName { get; set; }
    public string? ColorCode { get; set; }
    public string[]? ClassImages { get; set; }
    public decimal OneOffPrice { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal PaygPrice { get; set; }
    public required List<ScheduleInfoDto> ScheduleInfo { get; set; }
    public required string? GroupBy { get; set; }
    public string? AvailabilityStatus { get; set; }
    public bool IsWaitable { get; set; }
    public decimal HourlyPrice { get; set; }
}

public class ScheduleInfoDto
{
    public string? Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
