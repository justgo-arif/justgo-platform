using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.Features.ClubMembers.Queries.GetClubsByUserId;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Permissions.Queries.GetPermissionsByUserId
{
    public class GetPermissionsByUserIdHandler : IRequestHandler<GetPermissionsByUserIdQuery, List<AbacPermission>>
    {
        private readonly LazyService<IReadRepository<AbacPermission>> _readRepository;

        public GetPermissionsByUserIdHandler(LazyService<IReadRepository<AbacPermission>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<AbacPermission>> Handle(GetPermissionsByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT DISTINCT ap.[Id],ap.[Permission]
                            FROM [dbo].[AbacPermissions] ap
	                            INNER JOIN [dbo].[AbacRolePermissions] arp ON ap.[Id]=arp.[PermissionId]
	                            INNER JOIN [dbo].[AbacRoles] ar ON ar.Id=arp.[RoleId]
	                            INNER JOIN [dbo].[AbacUserRoles] aur ON aur.[RoleId]=ar.[Id]
                            WHERE aur.[UserId]=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var abacPermissions = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return abacPermissions;
        }
    }
}
