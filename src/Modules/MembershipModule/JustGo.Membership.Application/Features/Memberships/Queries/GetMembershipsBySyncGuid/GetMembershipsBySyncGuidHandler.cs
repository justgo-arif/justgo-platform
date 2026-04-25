using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMembershipsBySyncGuid
{
    public class GetMembershipsBySyncGuidHandler : IRequestHandler<GetMembershipsBySyncGuidQuery, List<Domain.Entities.Membership>>
    {
        private readonly LazyService<IReadRepository<Domain.Entities.Membership>> _readRepository;

        public GetMembershipsBySyncGuidHandler(LazyService<IReadRepository<Domain.Entities.Membership>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<Domain.Entities.Membership>> Handle(GetMembershipsBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT DISTINCT
	                            ht.HierarchyTypeName AS OrganisationName,
                                JSON_VALUE(membership.value, '$.name') AS MembershipName,
                                CONVERT(VARCHAR,TRY_CAST(JSON_VALUE(membership.value, '$.start') AS DATETIME), 23) AS StartDate,
                                CONVERT(VARCHAR,TRY_CAST(JSON_VALUE(membership.value, '$.end') AS DATETIME), 23) AS EndDate
                            FROM [dbo].[MembershipSummary] m
                                CROSS APPLY OPENJSON(m.[Entity1Memberships]) AS JsonData
                                CROSS APPLY OPENJSON(JsonData.value, '$.memberships') AS membership
                                INNER JOIN [User] u ON u.MemberDocId = m.[EntityId]
                                INNER JOIN [dbo].[Hierarchies] h ON h.EntityId=JSON_VALUE(membership.value, '$.ownerId')
                                INNER JOIN [dbo].[HierarchyTypes] ht ON ht.Id=h.HierarchyTypeId
                            WHERE u.UserSyncId=@UserSyncId
                                AND JSON_VALUE(membership.value, '$.status') = '1'
                                AND ht.[IsShared]=1";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncId", request.SyncGuid, dbType: DbType.Guid);
            var memberships = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return memberships;
        }
    }
}
