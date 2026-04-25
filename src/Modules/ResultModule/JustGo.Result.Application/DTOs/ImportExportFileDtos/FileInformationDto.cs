using Newtonsoft.Json;

namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class FileInformationDto
    {
        public int FileId { get; set; }
        public int DisciplineId { get; set; }
        public string DisciplineName { get; set; } = string.Empty;
        public int Records { get; set; } = 0;
        public int Errors { get; set; } = 0;
        public string? FileType { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? FileName { get; set; }
        public string? UploadedByImage { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal ErrorPercentage { get; set; }
        public decimal SuccessPercentage { get; set; }
        
        [JsonIgnore]
        public int TotalCount { get; set; }
    }
}
