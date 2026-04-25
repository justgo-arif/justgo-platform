using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Groups.Queries.GetGroupsByUserId
{
    public class GetGroupsByUserIdQuery : IRequest<List<Group>>
    {
        public GetGroupsByUserIdQuery(int userId)
        {
            UserId = userId;
        }

        public int UserId { get; set; }
    }
}
