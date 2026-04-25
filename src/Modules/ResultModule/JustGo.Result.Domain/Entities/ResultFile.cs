using System;

namespace JustGo.Result.Domain.Entities
{
    public class ResultFile
    {
        public int ResultFileId { get; set; }
        public int FileId { get; set; }
        public string? Discipline { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? SubmittedBy { get; set; }
    }
}
