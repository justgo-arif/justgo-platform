namespace JustGo.MemberProfile.Domain.Entities
{
    public class Organisation
    {
        public required string OwnerType { get; set; }
        public required string OrganisationName { get; set; }
        public string? OrganisationPicUrl { get; set; }
        public List<UserMembership> UserMemberships { get; set; } = new List<UserMembership>();
    }
}
