namespace JustGo.Booking.Domain.Entities;

public class BookingSession
{
    public int SessionId { get; set; }
    public bool AllSessionsFull { get; set; }
    public int AvailableFullBookQty { get; set; }
    public bool WaitlistOnly { get; set; }
    public int NoOfInvite { get; set; }
}
