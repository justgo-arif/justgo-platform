using System;
namespace JustGo.Authentication.Infrastructure.Files
{
    public class Attachment
    {
        public int AttachmentId { get; set; }
        public Guid AttachmentGuid { get; set; }
        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GeneratedName { get; set; } = string.Empty;
        public long Size { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ProfilePicURL { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }
}
