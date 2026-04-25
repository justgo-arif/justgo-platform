using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetWebletPreference;

public class GetWebletPreferenceHandler : IRequestHandler<GetWebletPreferenceQuery, WebletPreference>
{
    private readonly IReadRepositoryFactory _readRepository;
    public GetWebletPreferenceHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<WebletPreference> Handle(GetWebletPreferenceQuery request, CancellationToken cancellationToken)
    {
        var sql = """
                Select Top 1 U.MemberDocId, WP.PreferenceType, Isnull(wp.Value,'') PreferenceJsonValue
                From WebletPreference wp
                Inner join [User] u on u.Userid = wp.UserId 
                Where u.UserSyncId = @UserSyncId And wp.PreferenceType = @PreferenceType
                """;

        return await _readRepository.GetLazyRepository<WebletPreference>().Value.GetAsync(sql, cancellationToken, new { UserSyncId = request.UserSyncId, PreferenceType = request.PreferenceType }, null, "text");
    }

}
