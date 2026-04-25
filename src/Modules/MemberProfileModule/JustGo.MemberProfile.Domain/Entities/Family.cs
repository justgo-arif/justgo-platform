namespace JustGo.MemberProfile.Domain.Entities;

public class Family
{
    public string? Reference { get; set; }
    public required string FamilyName { get; set; }
    public Guid RecordGuid { get; set; }
    public List<FamilyMember> Members { get; set; } = [];
}
