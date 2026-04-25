using JustGo.MemberProfile.Application.DTOs.Enums;

namespace JustGo.MemberProfile.Application.DTOs;

public sealed class FamilyJoinRequestDto
{
    public int UserId { get; set; }
    public int UserFamilyId { get; set; }
    public int FamilyId { get; set; }
    public Guid RecordGuid { get; set; }
    public FamilyJoinRequestStatus Status { get; set; }
}