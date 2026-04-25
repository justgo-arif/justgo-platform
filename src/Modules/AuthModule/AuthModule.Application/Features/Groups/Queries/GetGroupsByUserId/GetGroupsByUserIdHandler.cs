using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Groups.Queries.GetGroupsByUserId
{
    public class GetGroupsByUserIdHandler : IRequestHandler<GetGroupsByUserIdQuery,List<Group>>
    {
        private readonly LazyService<IReadRepository<Group>> _readRepository;

        public GetGroupsByUserIdHandler(LazyService<IReadRepository<Group>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<Group>> Handle(GetGroupsByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT g.[GroupId]
                                      ,g.[Name]
                                      ,g.[Description]
                                      ,g.[IsActive]
                                      ,g.[Tag]
                                FROM [dbo].[Group] g 
	                                INNER JOIN [dbo].[GroupMembers] gm
		                                ON g.GroupId=gm.GroupId
	                                INNER JOIN [dbo].[User] u
		                                ON u.Userid=gm.UserId
                                WHERE g.IsActive=1
	                                AND u.Userid=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var groups = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return groups.AsList();
        }
    }
}
