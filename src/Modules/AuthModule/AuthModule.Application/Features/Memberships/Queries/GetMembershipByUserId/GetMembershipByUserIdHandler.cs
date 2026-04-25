using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Memberships.Queries.GetMembershipByUserId
{
    public class GetMembershipByUserIdHandler : IRequestHandler<GetMembershipByUserIdQuery, List<AuthModule.Domain.Entities.Membership>>
    {
        private readonly LazyService<IReadRepository<AuthModule.Domain.Entities.Membership>> _readRepository;

        public GetMembershipByUserIdHandler(LazyService<IReadRepository<AuthModule.Domain.Entities.Membership>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<AuthModule.Domain.Entities.Membership>> Handle(GetMembershipByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT 
	                        d.SyncGuid,
                            JSON_VALUE(membership.value, '$.name') AS [Name],
                            JSON_VALUE(membership.value, '$.category') AS Category
                        FROM [dbo].[MembershipSummary] m
                            CROSS APPLY OPENJSON(m.[Entity1Memberships]) AS JsonData
                            CROSS APPLY OPENJSON(JsonData.value, '$.memberships') AS membership
                            INNER JOIN [dbo].[Document] d ON JSON_VALUE(membership.value, '$.membershipDocId')=d.DocId
                            INNER JOIN [User] u ON u.MemberDocId = m.[EntityId]
                        WHERE u.Userid = @UserId
                            AND JSON_VALUE(membership.value, '$.status') = '1'";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var memberships = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return memberships.AsList();
        }
    }
}
