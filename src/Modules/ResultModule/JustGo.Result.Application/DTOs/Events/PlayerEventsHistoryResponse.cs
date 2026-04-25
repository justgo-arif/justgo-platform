namespace JustGo.Result.Application.DTOs.Events;

public class PlayerEventsHistoryResponse
{
    public List<PlayerEventHistoryDto> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class PlayerEventHistoryDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? County { get; set; } = string.Empty;
    public string? EventCategory { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int BeginRating { get; set; }
    public int FinalRating { get; set; }
    public int Difference => FinalRating - BeginRating;
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses => TotalMatches - TotalWins;
    public int TotalRecords { get; set; }

}


