using StackExchange.Redis;
using System;

namespace JustGo.Result.Application.DTOs.Events;


public class PlayerRankingsResponse
{
    public List<PlayerRankingDto> Players { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

public class PlayerRankingDto
{
    public int TotalRecords { get; set; }
    public int RatingRank { get; set; }
    public string PrefixedRank => "#" + RatingRank;
    public string PlayerId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string PrefixedPlayerId => "USATT# " + MemberId;
    public string PlayerName { get; set; } = string.Empty;
    public int ClubDocId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }
    public int FinalRating { get; set; }
    public string? PlayerImageUrl { get; set; }
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int TotalLost => TotalMatches - TotalWins;

}


