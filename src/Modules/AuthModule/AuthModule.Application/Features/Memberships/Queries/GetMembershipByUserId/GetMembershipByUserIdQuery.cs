using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Memberships.Queries.GetMembershipByUserId
{
    public class GetMembershipByUserIdQuery : IRequest<List<Membership>>
    {
        public GetMembershipByUserIdQuery(int userId)
        {
            UserId = userId;
        }

        public int UserId { get; set; }
    }
}
