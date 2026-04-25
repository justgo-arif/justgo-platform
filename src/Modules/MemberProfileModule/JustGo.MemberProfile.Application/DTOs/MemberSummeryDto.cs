namespace JustGo.MemberProfile.Application.DTOs
{
    public class MemberSummeryDto
    {
        public int Userid { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ProfilePicURL { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public int MemberDocId { get; set; }
    }
}
