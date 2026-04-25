using Microsoft.AspNetCore.Http;

namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public record UploadResultFileDto
{
    public required int EventId { get; init; }
    public required string OwnerGuid { get; set; }
    public int? OwnerId { get; set; }
    public required int DisciplineId { get; set; }
    public required IFormFile File { get; init; }
    public int? PreviousUploadedFileId { get; set; } = null;
}