namespace JustGo.Result.Application.DTOs.Events;


public class EventCompetitionResponse
{
    public EventData EventData { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}


public class EventData
{
    public int EventId { get; set; }
    public int RoundId { get; set; }
    public int CompetitionId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<CompetitionMatchDto> Matches { get; set; } = [];
}

public class MatchDto: CompetitionMatchDto
{
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public int TotalRecords { get; set; }
}


public class CompetitionMatchDto
{
    public int? MatchId { get; set; }
    public string? WinnerParticipantId { get; set; }
    public string WinnerMemberId { get; set; } = string.Empty;
    public string PrefixedWinnerId => "USATT# " + WinnerMemberId;
    public string WinnerName { get; set; } = string.Empty;
    public int WinnerBeginRating { get; set; }
    public int WinnerFinalRating { get; set; }
    public int WinnerRatingChange { get; set; }
    public RatingChangeStatus WinnerRatingChangeStatus { get; set; }
    public int WinnerDifference => WinnerFinalRating - WinnerBeginRating;
    public string? WinnerGender { get; set; }
    public string? WinnerImageUrl { get; set; }
    public string? LoserParticipantId { get; set; }
    public string LoserMemberId { get; set; } = string.Empty;
    public string PrefixedLoserId => "USATT# " + LoserMemberId;
    public string LoserName { get; set; } = string.Empty;
    public int LoserBeginRating { get; set; }
    public int LoserFinalRating { get; set; }
    public int LoserRatingChange { get; set; }
    public RatingChangeStatus LoserRatingChangeStatus { get; set; }
    public int LoserDifference => LoserFinalRating - LoserBeginRating;
    public string? LoserGender { get; set; }
    public string? LoserImageUrl { get; set; }
    public string? MatchScores { get; set; }
    public int CompetitionId { get; set; }
    public int RoundId { get; set; }
    public bool IsCompleted { get; set; }

}

public enum RatingChangeStatus
{
    NotProcessed = 0,
    Processed = 1,
    Overridden = 2
}
