namespace JustGo.Result.Application.DTOs.Events;

public class DeleteEventCompetitionResponse
{
    public int MatchId { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MatchInfo
{
    public int MatchId { get; set; }
    public int RoundId { get; set; }
    public int? CompetitionParticipantId { get; set; }
    public int? CompetitionParticipantId2 { get; set; }
}