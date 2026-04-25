using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Permissions.Queries.GetPermissionsByUserId
{
    public class GetPermissionsByUserIdQuery : IRequest<List<AbacPermission>>
    {
        public GetPermissionsByUserIdQuery(int userId)
        {
            UserId = userId;
        }

        public int UserId { get; set; }
    }
}
