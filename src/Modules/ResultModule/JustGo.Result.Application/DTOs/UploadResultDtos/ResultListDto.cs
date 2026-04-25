namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public record ResultListDto
{
    public int TotalCount { get; set; }
    public required string FileType { get; set; }
    public int UploadedFileId { get; set; }
    public required string FileName { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? Disciplines { get; set; }
    public string? ResultStatus { get; set; }
    public int Records { get; set; }
    public int EventId { get; set; }
    public int ErrorCount { get; set; }
    public decimal ErrorPercentage => Records == 0 ? 0 : Math.Round((decimal)(ErrorCount * 100) / Records, 2);
    public decimal SuccessPercentage => Records == 0 ? 0 : Math.Round((decimal)((Records - ErrorCount) * 100) / Records, 2);
}
