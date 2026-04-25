using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetClubDetails;

public class GetMemberPrimaryClubDetailsQueryHandler : IRequestHandler<GetMemberPrimaryClubDetailsQuery, IEnumerable<PrimaryClubDto>>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetMemberPrimaryClubDetailsQueryHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IEnumerable<PrimaryClubDto>> Handle(GetMemberPrimaryClubDetailsQuery request, CancellationToken cancellationToken)
    {
        return await GetMemberPrimaryClubDetailsAsync(request, cancellationToken) ?? throw new KeyNotFoundException("Primary Club not found");
    }

    private async Task<IEnumerable<PrimaryClubDto>> GetMemberPrimaryClubDetailsAsync(GetMemberPrimaryClubDetailsQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserSyncId", request.UserGuid);

        var sql = @"SELECT DISTINCT d.SyncGuid as Id, c.ClubName as ClubName , cd.MyRoles as RoleName
            FROM Clubs_Default c
            inner join Clubs_Links cl on c.DocId=cl.DocId
            inner join ClubMembers_Default cd on cl.EntityId=cd.DocId
            inner join [User] u on u.MemberId=cd.MemberId
            Inner join Document d on d.DocId=c.DocId
 
            WHERE u.UserSyncId= @UserSyncId 
            AND cd.IsPrimary = 1";

        var result = await _readRepository.GetLazyRepository<PrimaryClubDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
        return result ?? new List<PrimaryClubDto>();
    }
}


