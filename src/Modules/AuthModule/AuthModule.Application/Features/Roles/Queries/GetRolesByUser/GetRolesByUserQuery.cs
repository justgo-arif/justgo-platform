using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Roles.Queries.GetRolesByUser
{
    public class GetRolesByUserQuery:IRequest<List<Role>>
    {
        public string LoginId { get; set; }
        public GetRolesByUserQuery(string loginId) 
        {
            LoginId = loginId;
        }
    }
}
