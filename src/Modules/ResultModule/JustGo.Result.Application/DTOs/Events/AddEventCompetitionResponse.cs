namespace JustGo.Result.Application.DTOs.Events;

public class AddEventCompetitionResponse
{
    public int MatchId { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public CompetitionMatchDto? MatchDetails { get; set; }
}