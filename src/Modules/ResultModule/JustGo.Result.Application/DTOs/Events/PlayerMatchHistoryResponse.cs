namespace JustGo.Result.Application.DTOs.Events;

public class PlayerMatchHistoryResponse
{
    public PlayerSummaryDto PlayerSummary { get; set; } = new();
    public List<PlayerMatchHistoryDto> Matches { get; set; } = new();
    public DateTime? StartDateTime { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string FormattedStartDate => StartDateTime.HasValue ? StartDateTime.Value.ToString("dd MMM yyyy") : string.Empty;
    public string FormattedStartTime => StartDateTime.HasValue ? StartDateTime.Value.ToString("HH:mm") : string.Empty;
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class PlayerSummaryDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string? PlayerGender { get; set; }
    public string? Country { get; set; }
}

public class PlayerMatchHistoryDto
{
    public int MatchId { get; set; }
    public int CompetitionId { get; set; }
    public int RoundId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime? MatchDate { get; set; }
    public DateTime? StartDateTime { get; set; }

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
    public string? WinnerImageUrl { get; set; }
    public string? LoserImageUrl { get; set; }

    public string? MatchScores { get; set; }
    public bool IsCompleted { get; set; }
    public int TotalRecords { get; set; }
}

