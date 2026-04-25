namespace JustGo.Organisation.Application.DTOs;

public class MyOrganisationDto
{
    public int ClubDocId { get; set; }
    public required string ClubSyncGuid { get; set; }
    public string? ClubImagePath { get; set; }
    public required string ClubName { get; set; }
    public required string ClubId { get; set; }
    public string? EmailAddress { get; set; }
    public string? ClubWebSite { get; set; }
    public string? LatLng { get; set; }
    public int ClubMemberDocId { get; set; }
    public bool IsLowestTier { get; set; }
    public string? SocialLinks { get; set; }
    public string? TransferMessage { get; set; }
    public string? TransferSyncGuid { get; set; }
    public string? MemberStatus { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsTransfer { get; set; }
    public string[]? Roles { get; set; }
    public string[]? Parents { get; set; }
}

