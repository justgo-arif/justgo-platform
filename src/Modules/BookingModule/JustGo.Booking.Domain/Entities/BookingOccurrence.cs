namespace JustGo.Booking.Domain.Entities;

public class BookingOccurrence
{
    public int SessionId { get; set; }
    public int Capacity { get; set; }
    public int OccurrenceId { get; set; }
    public int ScheduleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsTrialable { get; set; }
    public bool IsFuture { get; set; }
    public int BookedQty { get; set; }
    public int TrialReserveQty { get; set; }
}
