namespace JustGo.Result.Application.DTOs.Events;

public class UpdateCompetitionResponse
{
    public int EventId { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}