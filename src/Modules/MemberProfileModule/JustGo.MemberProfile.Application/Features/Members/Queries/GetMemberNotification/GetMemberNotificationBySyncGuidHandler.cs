using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberNotification;

public class GetMemberNotificationBySyncGuidHandler : IRequestHandler<GetMemberNotificationBySyncGuidQuery, List<UserNotificationDto>>
{
    private readonly IReadRepositoryFactory _readRepository;
    public GetMemberNotificationBySyncGuidHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<List<UserNotificationDto>> Handle(GetMemberNotificationBySyncGuidQuery request, CancellationToken cancellationToken = default)
    {
        return (await _readRepository.GetLazyRepository<UserNotificationDto>().Value.GetListAsync("GetUserNotification", cancellationToken, new { UserGuid = request.Id })).ToList();
    }

}
