using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class MemberNotes
    {
        public int MemberNoteId { get; set; }
        public Guid MemberNoteGuid { get; set; } = Guid.NewGuid();
        public string EntityType { get; set; } = "8";
        public required string EntityId { get; set; } //member id from ui
        public string? MemberNoteTitle { get; set; }
        public string? Details { get; set; }
        public int UserId { get; set; } //logged in user id
        public DateTime CreatedDate { get; set; }
        public int OwnerId { get; set; } // club id
        public int NoteCategoryId { get; set; }
        public bool IsActive { get; set; }
        public bool IsHide { get; set; }
    }
}
