using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberNotification;

public class GetMemberNotificationBySyncGuidQuery : IRequest<List<UserNotificationDto>>
{
    public Guid Id { get; set; }
    public GetMemberNotificationBySyncGuidQuery(Guid id)
    {
        Id = id;
    }
}
