namespace JustGo.MemberProfile.Domain.Entities;

public class FamilyMember
{
    public Guid RecordGuid { get; set; }
    public int UserFamilyId { get; set; }
    public int FamilyId { get; set; }
    public bool IsAdmin { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string MemberId { get; set; }
    public string? EmailAddress { get; set; }
    public int UserId { get; set; }
    public Guid UserSyncId { get; set; }
    public string? ProfilePicURL { get; set; }
    public bool IsPendingApproval { get; set; } = false;
    public string[] Memberships { get; set; } = [];
}
