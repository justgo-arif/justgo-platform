using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.DTOs.Enums;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyJoinRequest;

public sealed class GetFamilyJoinRequestQueryHandler : IRequestHandler<GetFamilyJoinRequestQuery, List<FamilyJoinRequestDto>>
{
    private readonly LazyService<IReadRepository<object>> _readRepository;
    private readonly IUtilityService _utilityService;

    public GetFamilyJoinRequestQueryHandler(LazyService<IReadRepository<object>> readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }

    public async Task<List<FamilyJoinRequestDto>> Handle(GetFamilyJoinRequestQuery request, CancellationToken cancellationToken)
    {
        const string sql = """

            SELECT uf.UserId,UserFamilyId,uf.FamilyId,uf.RecordGuid,[Status] FROM UserFamilies uf
            INNER JOIN Families f on f.FamilyId=uf.FamilyId
            INNER JOIN [User] u on u.UserId=uf.UserId
            WHERE u.UserSyncId=@UserSyncGuid AND uf.[Status]=0
            AND u.Userid=@ActionUserId      
            """;

        var param = new DynamicParameters();
        var currentUser = await _utilityService.GetCurrentUserPublic(cancellationToken);
        if (currentUser == null)
        {
            throw new InvalidOperationException("Current user cannot be null.");
        }
        param.Add("@UserSyncGuid", request.Id, DbType.Guid);
        param.Add("@ActionUserId", currentUser.UserId);

        var rows = await _readRepository.Value.GetListAsync(sql, cancellationToken, param, null, "text");

        var result = rows.Select(static r =>
        {
            var dict = (IDictionary<string, object>)r;

            var statusInt = Convert.ToInt32(dict["Status"]);
            var status = Enum.IsDefined(typeof(FamilyJoinRequestStatus), statusInt)
                ? (FamilyJoinRequestStatus)statusInt
                : FamilyJoinRequestStatus.Pending;

            return new FamilyJoinRequestDto
            {
                UserId = Convert.ToInt32(dict["UserId"]),
                UserFamilyId = Convert.ToInt32(dict["UserFamilyId"]),
                FamilyId = Convert.ToInt32(dict["FamilyId"]),
                RecordGuid = (Guid)dict["RecordGuid"],
                Status = status
            };
        }).ToList();

        return result;
    }
}