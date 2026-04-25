namespace JustGo.Result.Application.DTOs.Events
{
    public class PlayerPerformanceGlobalStatsResponse
    {
        public string MemberId { get; set; }
        public List<StatItem> Stats { get; set; }
    }

    public class PlayerInfo
    {
        public string MemberId { get; set; }
        public int HighestRating { get; set; }
        public int CurrentRating { get; set; }
        public int TotalMatches { get; set; }
        public int TotalWins { get; set; } = 0;
        public decimal WinPercentage => TotalMatches > 0 ? Math.Round((decimal)TotalWins / TotalMatches * 100, 2) : 0;
    }

    

    public class StatItem
    {
        public string Label { get; set; }
        public string Icon { get; set; }
        public decimal Value { get; set; }
    }
}
