namespace JustGo.MemberProfile.Application.DTOs;

public record FindMemberDto
{
    public int MemberDocId { get; init; }
    public required string MID { get; init; }
    public int UserId { get; init; }
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Surname { get; init; }
    public string? EmailAddress { get; init; }
    public string? Gender { get; init; }
    public string? ProfilePicURL { get; init; }
    public DateTime? DOB { get; init; }
    public Guid UserSyncId { get; init; }
}
