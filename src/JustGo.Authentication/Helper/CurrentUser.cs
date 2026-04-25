namespace JustGo.Authentication.Helper
{
    public class CurrentUser
    {
        public int UserId { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public int MemberDocId { get; set; }
        public string MemberId { get; set; } = string.Empty;
    }
}
