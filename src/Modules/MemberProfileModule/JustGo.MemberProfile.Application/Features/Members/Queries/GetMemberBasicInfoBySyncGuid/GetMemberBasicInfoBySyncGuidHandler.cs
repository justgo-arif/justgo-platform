using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using MapsterMapper;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberBasicInfoBySyncGuid;

public class GetMemberBasicInfoBySyncGuidHandler : IRequestHandler<GetMemberBasicInfoBySyncGuidQuery, MemberBasicInfo?>
{
    private readonly LazyService<IReadRepository<MemberBasicInfo>> _readRepository;
    private readonly IMapper _mapper;
    public GetMemberBasicInfoBySyncGuidHandler(LazyService<IReadRepository<MemberBasicInfo>> readRepository, IMapper mapper)
    {
        _readRepository = readRepository;
        _mapper = mapper;
    }

    public async Task<MemberBasicInfo?> Handle(GetMemberBasicInfoBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        string sql = """
        SELECT U.FirstName, U.LastName, U.CreationDate, U.LastLoginDate, U.IsActive, U.IsLocked, U.EmailAddress, U.ProfilePicURL, 
        U.DOB, U.Gender, U.Address1, U.Address2, U.Address3, U.Town, U.County, U.Country, U.PostCode, U.EmailVerified, U.MemberId, U.UserSyncId, 
        U.SuspensionLevel, U.LoginId, U.UserId, U.MemberDocId
        FROM [User] U
        WHERE U.UserSyncId = @UserSyncId
        """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserSyncId", request.Id, dbType: DbType.Guid);
        var member = await _readRepository.Value.GetAsync(sql, cancellationToken, new { UserSyncId = request.Id }, null, "text");
        return member;
    }
}
