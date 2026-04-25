namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public record MemberDataDto
{
    public string MemberId { get; set; } = string.Empty;
    public string? MemberProfilePicUrl { get; set; }
    public string? Mobile { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Memberships { get; set; } = string.Empty;
    public int MembershipCount { get; set; }
    public string Expires { get; set; } = string.Empty;
    public string MemberData { get; set; } = string.Empty;
}