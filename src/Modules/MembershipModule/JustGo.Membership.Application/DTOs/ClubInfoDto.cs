namespace JustGo.Membership.Application.DTOs
{
    public class ClubInfoDto
    {
        public int ClubDocId { get; set; }
        public string ClubSyncGuid { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string ClubId { get; set; } = string.Empty;
        public bool IsJoinedClub { get; set; }
        public string Image { get; set; } = string.Empty;
        public string HierarchyTypeName { get; set; }=string.Empty;
        public List<LicenseDto> Licenses { get; set; } = new();
    }
}
