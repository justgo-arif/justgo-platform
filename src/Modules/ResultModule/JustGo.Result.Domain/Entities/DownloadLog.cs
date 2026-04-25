using System;

namespace JustGo.Result.Domain.Entities
{
    public class DownloadLog
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public string? DownloadedBy { get; set; }
        public DateTime DownloadedAt { get; set; }
    }
}
