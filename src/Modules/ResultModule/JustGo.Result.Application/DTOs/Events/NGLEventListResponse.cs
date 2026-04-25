namespace JustGo.Result.Application.DTOs.Events;

public class NGLEventListResponse 
{
    public List<NGLEventSummaryDto> Events { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class NGLEventSummaryDto : EventListBaseDto
{
    public int TotalParticipants { get; set; }
    public string? DisciplineName { get; set; }

}
