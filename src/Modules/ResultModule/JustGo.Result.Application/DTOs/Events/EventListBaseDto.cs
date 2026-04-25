namespace JustGo.Result.Application.DTOs.Events;
public abstract class EventListBaseDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? County { get; set; } = string.Empty;
    public string? EventImageUrl { get; set; } = string.Empty;
    public string? EventCategory { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string FormattedStartDate => StartDateTime.ToString("dd MMM yyyy");
    public string FormattedStartTime => StartDateTime.ToString("HH:mm");
    public int TotalRecords { get; set; }

}