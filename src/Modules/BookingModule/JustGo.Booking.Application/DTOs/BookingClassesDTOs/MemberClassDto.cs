namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs;

public class MemberClassDto
{
    public required string ClassGroupName { get; set; }
    public required string ClassName { get; set; }
    public required string ClassGuid { get; set; }
    public required string VenueName { get; set; }
    public string[]? Coaches { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal BookingAmount { get; set; }
    public string? ProductType { get; set; }
    public string[]? ClassImages { get; set; }
    public int NoOfClasses { get; set; }
    public string? ClubName { get; set; }
    public string? ClubImageUrl { get; set; }
}
