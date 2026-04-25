using Newtonsoft.Json;

namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public record EventDto
{
    [JsonIgnore]
    public int TotalCount { get; init; }
    public int EventId { get; init; }
    public string ResultEventTypeId { get; init; } = string.Empty;
    public string ResultEventType { get; init; } = string.Empty;
    public required string EventReference { get; init; }
    public required string EventName { get; init; }
    public  string SourceType { get; init; }
    
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    
    public string? DisciplineNames { get; init; }

    public int DraftCount { get; init; }
    
    public int PublishedCount { get; init; }
}


