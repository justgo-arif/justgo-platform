namespace JustGo.Result.Application.DTOs.Events;

public class UpdateResultCompetitionRankingResponse
{
    public Guid RecordGuid { get; init; }
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
}

