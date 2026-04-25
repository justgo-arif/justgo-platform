namespace JustGo.Booking.Domain.Entities;

public class MemberClass
{
    public required string ClassGroupName { get; set; }
    public required string ClassName { get; set; }
    public required string ClassGuid { get; set; }
    public required string VenueName { get; set; }
    public string? Coaches { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal BookingAmount { get; set; }
    public int ProductType { get; set; }
    public string? ClassImages { get; set; }
    public int TotalRows { get; set; }
    public int RowNumber { get; set; }
    public int NoOfClasses { get; set; }
    public string? ClubName { get; set; }
    public string? ClubImageUrl { get; set; }

}
