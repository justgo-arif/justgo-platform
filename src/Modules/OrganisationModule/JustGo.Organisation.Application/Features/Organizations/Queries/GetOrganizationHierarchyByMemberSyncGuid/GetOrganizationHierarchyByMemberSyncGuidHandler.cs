using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Domain.Entities;
using System.Data;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetOrganizationHierarchyByMemberSyncGuid;

public class GetOrganizationHierarchyByMemberSyncGuidHandler : IRequestHandler<GetOrganizationHierarchyByMemberSyncGuidQuery, List<HierarchyType>>
{
    private readonly LazyService<IReadRepository<HierarchyType>> _readRepository;

    public GetOrganizationHierarchyByMemberSyncGuidHandler(LazyService<IReadRepository<HierarchyType>> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<List<HierarchyType>> Handle(GetOrganizationHierarchyByMemberSyncGuidQuery request, CancellationToken cancellationToken)
    {
        string sql = @"SELECT DISTINCT ht.[Id],ht.[HierarchyTypeName]
                                FROM [dbo].[HierarchyTypes] ht
	                                INNER JOIN [dbo].[Hierarchies] h ON ht.Id=h.HierarchyTypeId
	                                INNER JOIN [dbo].[HierarchyLinks] hl ON h.Id=hl.HierarchyId
	                                INNER JOIN [User] u ON u.Userid=hl.UserId
                                WHERE ht.[LevelNo]>0
                                    AND ht.[IsShared]=1
                                    AND u.UserSyncId=@UserSyncId";
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserSyncId", request.SyncGuid, dbType: DbType.Guid);
        var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
        return result;
    }
}
