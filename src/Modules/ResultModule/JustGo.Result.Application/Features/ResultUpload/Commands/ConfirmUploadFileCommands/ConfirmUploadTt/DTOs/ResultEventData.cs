namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadTt.DTOs;

public record ResultEventData
{
    public int DisciplineId { get; set; }
    public int EventId { get; set; }
    public required string EventName { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? FileCategory { get; set; }
}