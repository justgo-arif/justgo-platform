namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class FindMembersDto
    {
        public int UserId { get; set; }
        public string UserGuid { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string MembershipName { get; set; } = string.Empty;
        public int MembershipTypeCount { get; set; }
    }
}
