namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMemberMemberships
{
    public class MembersHierarchiesWithMemberships
    {
        public int HierarchyTypeId { get; set; }
        public string HierarchyTypeName { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string OrganisationPicUrl { get; set; } = string.Empty;
        public Guid? OrganisationGuid { get; set; } 
        public List<MemberMemberships> MemberMemberships { get; } = new();
    }
}