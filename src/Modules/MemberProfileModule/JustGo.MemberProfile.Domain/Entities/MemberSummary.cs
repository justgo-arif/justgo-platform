namespace JustGo.MemberProfile.Domain.Entities;

public class MemberSummary
{
    public int UserId { get; set; }
    public int MemberDocId { get; set; }
    public required string LoginId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Mobile { get; set; }
    public string? CountryCode { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? EmailAddress { get; set; }
    public string? ProfilePicURL { get; set; }
    public DateTime? DOB { get; set; }
    public string? Gender { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? Town { get; set; }
    public string? County { get; set; }
    public string? Country { get; set; }
    public string? PostCode { get; set; }
    public DateTime? EmailVerified { get; set; }
    public required string MemberId { get; set; }
    public Guid? UserSyncId { get; set; }
    public int SuspensionLevel { get; set; }
    public string? PrimaryMembership { get; set; }
    public int CountryId { get; set; }
    public int CountyId { get; set; }
}
