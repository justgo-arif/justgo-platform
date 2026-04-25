using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.DTOs
{
    public class GetMemberNotesDto
    {
        public List<GetMemberNotesDataDto> MemberNotes { get; set; } = [];
        public int TotalCounts { get; set; }
    }
    public class GetMemberNotesDataDto
    {
        public int MemberNoteId { get; set; }
        public Guid MemberNoteGuid { get; set; } 
        public string MemberName { get; set; } = null!;
        public string ProfilePicUrl { get; set; } = null!;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string NoteTitle { get; set; } = null!;
        public string Details { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsHide { get; set; }
    }
}
