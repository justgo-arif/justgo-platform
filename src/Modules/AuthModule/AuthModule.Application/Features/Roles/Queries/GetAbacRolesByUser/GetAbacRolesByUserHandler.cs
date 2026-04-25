using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.Features.Roles.Queries.GetRolesByUser;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Roles.Queries.GetAbacRolesByUser
{
    public class GetAbacRolesByUserHandler : IRequestHandler<GetAbacRolesByUserQuery, List<Role>>
    {
        private readonly LazyService<IReadRepository<Role>> _readRepository;

        public GetAbacRolesByUserHandler(LazyService<IReadRepository<Role>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<Role>> Handle(GetAbacRolesByUserQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT DISTINCT r.[Id],r.[Name],r.[Description],r.[Status]
                                FROM [dbo].[AbacRoles] r
	                                INNER JOIN [dbo].[AbacUserRoles] ur ON r.[Id]=ur.[RoleId]
                                WHERE ur.[UserId]=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var Roles = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return Roles.AsList();
        }
    }
}
