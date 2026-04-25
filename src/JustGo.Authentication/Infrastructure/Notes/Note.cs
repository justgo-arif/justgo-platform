using System;

namespace JustGo.Authentication.Infrastructure.Notes
{
    public class Note
    {
        public int NotesId { get; set; }
        public Guid NotesGuid { get; set; }
        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ProfilePicURL { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }
}
