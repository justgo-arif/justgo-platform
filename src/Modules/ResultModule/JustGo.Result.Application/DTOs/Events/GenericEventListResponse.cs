namespace JustGo.Result.Application.DTOs.Events;

public class GenericEventListResponse 
{
    public List<GenericEventSummaryDto> Events { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public required bool ShowAssets { get; set; }
    public required string ParticipantPlaceHolder { get; set; }
}

public class GenericEventSummaryDto : EventListBaseDto
{
    public int TotalParticipants { get; set; }
    public int TotalAssets { get; set; }
    public string? DisciplineName { get; set; }

}