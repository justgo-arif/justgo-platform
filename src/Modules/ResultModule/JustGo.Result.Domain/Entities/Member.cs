using System;

namespace JustGo.Result.Domain.Entities
{
    public class Member
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
