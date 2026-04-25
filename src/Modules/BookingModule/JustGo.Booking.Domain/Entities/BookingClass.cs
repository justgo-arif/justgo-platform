namespace JustGo.Booking.Domain.Entities;

public class BookingClass
{
    public int SessionId { get; set; }
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
    public string? Gender { get; set; }
    public string? ColorName { get; set; }
    public string? ColorCode { get; set; }
    public string? ClassImages { get; set; }
    public decimal OneOffPrice { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal HourlyPrice { get; set; }
    public decimal PaygPrice { get; set; }
    public required string ScheduleInfo { get; set; }
    public int TotalRows { get; set; }
    public int RowNumber { get; set; }
    public string? DayOfWeek { get; set; }
}
