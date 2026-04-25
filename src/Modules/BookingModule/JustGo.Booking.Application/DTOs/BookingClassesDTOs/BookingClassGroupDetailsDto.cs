namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs;

public class BookingClassGroupDetailsDto
{
    public int ClassGroupId { get; set; }
    public required string ClubGuid { get; set; }
    public required string CategoryGuid { get; set; }
}
