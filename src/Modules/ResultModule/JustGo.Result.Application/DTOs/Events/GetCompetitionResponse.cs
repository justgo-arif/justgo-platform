namespace JustGo.Result.Application.DTOs.Events;

public class GetCompetitionResponse
{
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CategoryId { get; set; }
    public int? ResultEventTypeId { get; set; }
    public string? TimeZone { get; set; }
    public string? ImagePath { get; set; }
    public string? Postcode { get; set; }
    public string? County { get; set; }
    public string? Town { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }

}