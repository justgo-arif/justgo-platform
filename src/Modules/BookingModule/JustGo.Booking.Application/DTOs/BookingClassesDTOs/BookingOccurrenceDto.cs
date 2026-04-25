namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs;

public class BookingOccurrenceDto
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
    public int AvailableQty => Math.Max(Capacity - (BookedQty+ TrialReserveQty), 0);
    public int StartDay => StartDate.Day;
    public string StartDayInString => StartDate.ToString("ddd").ToUpper();
    public string StartTime => StartDate.ToString("hh:mm tt");
    public string EndTime => EndDate.ToString("h:mm tt");
}
