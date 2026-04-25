namespace JustGo.MemberProfile.Application.DTOs
{
    public class UserBasicInfoDTO
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int MemberDocId { get; set; }
        public string memberId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
