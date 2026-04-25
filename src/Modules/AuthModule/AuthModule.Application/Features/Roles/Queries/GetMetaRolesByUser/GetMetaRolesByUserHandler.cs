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

namespace AuthModule.Application.Features.Roles.Queries.GetMetaRolesByUser
{
    public class GetMetaRolesByUserHandler : IRequestHandler<GetMetaRolesByUserQuery, List<Role>>
    {
        private readonly LazyService<IReadRepository<Role>> _readRepository;

        public GetMetaRolesByUserHandler(LazyService<IReadRepository<Role>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<Role>> Handle(GetMetaRolesByUserQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT DISTINCT RoleName AS [Name] FROM [dbo].[ClubMemberRoles]
                                WHERE UserId=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var Roles = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return Roles;
        }
    }
}
