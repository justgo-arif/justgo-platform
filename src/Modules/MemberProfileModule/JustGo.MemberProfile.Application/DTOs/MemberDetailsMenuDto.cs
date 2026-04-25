namespace JustGo.MemberProfile.Application.DTOs
{
    public class MemberDetailsMenuDto
    {
        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
        public string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; } = 0;
    }
}
