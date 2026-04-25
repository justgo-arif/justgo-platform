namespace JustGo.Finance.Application.DTOs.MemberDTOs
{
    public class UserDocumentInfo
    {
        public int DocId { get; set; }
        public required string MID { get; set; }
        public string? UserSyncId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string ImageSrc { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }

}
