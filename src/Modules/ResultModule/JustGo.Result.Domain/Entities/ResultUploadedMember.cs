using System;

namespace JustGo.Result.Domain.Entities
{
    public class ResultUploadedMember
    {
        public int UploadedMemberId { get; set; }
        public int UploadedFileId { get; set; }
        public int UserId { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public bool IsValidated { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public bool Modified { get; set; }
        public bool IsDeleted { get; set; }
    }
}
