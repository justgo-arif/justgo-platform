namespace JustGo.MemberProfile.Application.DTOs;

public sealed class FamilyRequestDetailsDto
{
    public Guid RecordGuid { get; set; }
    public required string FamilyName { get; set; }
    public required string ProfilePicURL { get; set; }
    public required string RequestLogged { get; set; }
}