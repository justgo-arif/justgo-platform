namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public record ResultCompetitionStatus
{
    public int StatusId { get; set; }
    public required string StatusName { get; set; }
}