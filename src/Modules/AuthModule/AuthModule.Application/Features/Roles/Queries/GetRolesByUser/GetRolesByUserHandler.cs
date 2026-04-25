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

namespace AuthModule.Application.Features.Roles.Queries.GetRolesByUser
{
    public class GetRolesByUserHandler : IRequestHandler<GetRolesByUserQuery, List<Role>>
    {
        private readonly LazyService<IReadRepository<Role>> _readRepository;

        public GetRolesByUserHandler(LazyService<IReadRepository<Role>> readRepository)
        {
            this._readRepository = readRepository;
        }
        public async Task<List<Role>> Handle(GetRolesByUserQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT DISTINCT r.[Name] FROM [dbo].[Role] r
	                                INNER JOIN [dbo].[RoleMembers] rm ON r.RoleId=rm.RoleId
	                                INNER JOIN [dbo].[User] u ON u.Userid=rm.UserId
                                WHERE u.LoginId=@LoginId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LoginId", request.LoginId);
            var Roles = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return Roles;
        }
    }
}
