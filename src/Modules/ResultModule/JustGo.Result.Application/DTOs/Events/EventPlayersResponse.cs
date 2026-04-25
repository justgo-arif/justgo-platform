namespace JustGo.Result.Application.DTOs.Events;

public class EventPlayersResponse
{
    public List<EventPlayerDto> Players { get; set; } = [];
    public int TotalCount { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime? StartDateTime { get; set; }
    public string FormattedStartDate => StartDateTime.HasValue ? StartDateTime.Value.ToString("dd MMM yyyy") : string.Empty;
    public string FormattedStartTime => StartDateTime.HasValue ? StartDateTime.Value.ToString("HH:mm") : string.Empty;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasMore => (PageNumber * PageSize) < TotalCount;
}

public class EventPlayerDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string PrefixedPlayerId => "USATT# " + MemberId;
    public string PlayerName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? RecordGuid { get; set; }
    public int BeginRating { get; set; }
    public int FinalRating { get; set; }
    public string? Gender { get; set; }
    public string? PlayerImageUrl { get; set; }
    public int Points => FinalRating - BeginRating; 
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int TotalLost => TotalMatches - TotalWins;
    public decimal WinPercentage => TotalMatches > 0 ? Math.Round((decimal)TotalWins / TotalMatches * 100, 2) : 0;

    public int EventId { get; set; }
    public int CompetitionId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime? StartDateTime { get; set; }
    public int FuzzyScore { get; set; }
    public int TotalRecords { get; set; }
}
