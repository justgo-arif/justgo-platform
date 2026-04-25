using System;

namespace JustGo.Result.Domain.Entities
{
    public class MemberValidationLog
    {
        public int Id { get; set; }
        public int? UploadedMemberId { get; set; } 
        public string MemberId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
    }
}
