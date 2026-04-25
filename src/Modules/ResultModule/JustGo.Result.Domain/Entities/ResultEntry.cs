using System;

namespace JustGo.Result.Domain.Entities
{
    public class ResultEntry
    {
        public int EntryId { get; set; }
        public int ResultFileId { get; set; }
        public int EntityId { get; set; }
        public string? ResultData { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}
