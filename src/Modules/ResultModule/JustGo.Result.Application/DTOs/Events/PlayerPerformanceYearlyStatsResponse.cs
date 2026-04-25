namespace JustGo.Result.Application.DTOs.Events
{
    public class PlayerPerformanceYearlyStatsResponse
    {
        public string MemberId { get; set; }
        public List<StatItemYearlyStats> Stats { get; set; }
    }

    public class PlayerInfoYearlyStats
    {
        public string MemberId { get; set; }
        public int HighestRating { get; set; }
        public int YearEndRating { get; set; }
        public int TotalTournaments { get; set; }
        public int TotalMatches { get; set; }
        public int TotalWins { get; set; } = 0;
        public int TotalLoss => TotalMatches - TotalWins;
    }

    

    public class StatItemYearlyStats
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Icon { get; set; }
        public decimal Value { get; set; }
    }
}
