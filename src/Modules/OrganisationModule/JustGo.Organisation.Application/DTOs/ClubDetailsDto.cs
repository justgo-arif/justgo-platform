namespace JustGo.Organisation.Application.DTOs;

public class ClubDetailsDto : ClubDto
{
    public required string ClubId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? ClubWebSite { get; set; }
    public string? SocialLinks { get; set; }
    public DateTime? JoinedDate { get; set; }
    public string? MemberStatus { get; set; }
    public string? MemberRoles { get; set; }
    public bool IsPrimary { get; set; }
    public string[] Roles => MemberRoles?.Split(",") ?? Array.Empty<string>();
    public int? TransferDocId { get; set; }
    public bool IsTransfer => this.TransferDocId is not null;
    public string? TransferClubName { get; set; }
}
