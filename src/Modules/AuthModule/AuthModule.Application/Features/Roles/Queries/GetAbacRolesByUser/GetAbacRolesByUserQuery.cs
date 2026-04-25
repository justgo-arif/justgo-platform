using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Roles.Queries.GetAbacRolesByUser
{
    public class GetAbacRolesByUserQuery : IRequest<List<Role>>
    {
        public GetAbacRolesByUserQuery(string userId)
        {
            UserId = userId;
        }

        public string UserId { get; set; }
    }
}
