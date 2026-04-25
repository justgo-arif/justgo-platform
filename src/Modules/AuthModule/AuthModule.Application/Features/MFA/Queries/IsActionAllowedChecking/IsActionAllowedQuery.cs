using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetTimeZoneValue;

public class IsActionAllowedQuery : IRequest<bool>
{
    public int InvokingUserId { get; set; }
    public int MemberDocId { get; set; }
    public string Option { get; set; } = "";

}
