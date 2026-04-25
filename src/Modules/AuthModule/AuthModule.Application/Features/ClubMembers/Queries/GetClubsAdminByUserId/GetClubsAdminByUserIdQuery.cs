using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.ClubMembers.Queries.GetClubsAdminByUserId
{
    public class GetClubsAdminByUserIdQuery : IRequest<List<Club>>
    {
        public GetClubsAdminByUserIdQuery(int userId)
        {
            UserId = userId;
        }

        public int UserId { get; set; }
    }
}
