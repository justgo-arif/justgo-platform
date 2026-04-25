namespace JustGo.Result.Application.DTOs.Events;



public class PlayerProfileDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string PrefixedPlayerId =>"USATT# "+ MemberId;
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? Gender { get; set; }
    public int? PrimaryClubId { get; set; }
    public string? PrimaryClubName { get; set; }
    public string? PlayerImageUrl { get; set; }
    public int HighestRating { get; set; }
    public int HighestLeagueRating { get; set; }
    public int TotalTournaments { get; set; }
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses => TotalMatches - TotalWins;
    public decimal WinPercentage => TotalMatches > 0 ? Math.Round((decimal)TotalWins / TotalMatches * 100, 2) : 0;
    public int RatingPointsGained { get; set; }
    public string FirstTournamentDate { get; set; } = string.Empty;
    public string LastTournamentDate { get; set; } = string.Empty;
    public DateTime? DOB { get; set; }

}



