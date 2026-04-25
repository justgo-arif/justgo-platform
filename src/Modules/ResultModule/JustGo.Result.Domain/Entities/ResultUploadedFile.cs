using System;

namespace JustGo.Result.Domain.Entities
{
    public class ResultUploadedFile
    {
        public int UploadedFileId { get; set; }
        public int OwnerId { get; set; }
        public int DisciplineId { get; set; } 
        public int? EventId { get; set; } = null;
        public string? FileType { get; set; }
        public string? FileCategory { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? FileName { get; set; }
        public string? Notes { get; set; }
        public string? BlobLocation { get; set; }
        public bool IsFinal { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
    }
}
