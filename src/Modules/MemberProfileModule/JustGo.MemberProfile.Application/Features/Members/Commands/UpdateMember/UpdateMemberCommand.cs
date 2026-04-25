using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.UpdateMember;

public class UpdateMemberCommand : IRequest<OperationResultDto<MemberSummaryDto>>
{
    public required string LoginId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Mobile { get; set; }
    public string? CountryCode { get; set; }
    public required string EmailAddress { get; set; }
    public required DateTime DOB { get; set; }
    public required string Gender { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? Town { get; set; }
    public string? County { get; set; }
    public required string Country { get; set; }
    public string? PostCode { get; set; }
    public int CountryId { get; set; }
    public int? CountyId { get; set; }
    public Guid UserSyncId { get; set; }
}
