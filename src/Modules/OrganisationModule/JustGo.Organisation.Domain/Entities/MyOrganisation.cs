namespace JustGo.Organisation.Domain.Entities;

public class MyOrganisation
{
    public int ClubDocId { get; set; }
    public required string ClubSyncGuid { get; set; }
    public string? Image { get; set; }
    public required string ClubName { get; set; }
    public required string ClubId { get; set; }
    public string? EmailAddress { get; set; }
    public string? ClubWebSite { get; set; }
    public string? LatLng { get; set; }
    public int ClubMemberDocId { get; set; }
    public bool IsLowestTier { get; set; }
    public string? SocialLinks { get; set; }
    public string? TransferSyncGuid { get; set; }
    public int? TransferDocId { get; set; }
    public string? TransferClubName { get; set; }
    public string? ParentChain { get; set; }
    public string? MemberStatus { get; set; }
    public string? MemberRoles { get; set; }
    public bool IsPrimary { get; set; }
}
