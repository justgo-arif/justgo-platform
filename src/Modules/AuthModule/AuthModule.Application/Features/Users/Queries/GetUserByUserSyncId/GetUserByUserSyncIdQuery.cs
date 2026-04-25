using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId
{
    public class GetUserByUserSyncIdQuery : IRequest<User>
    {
        public Guid UserSyncId { get; set; }
        public GetUserByUserSyncIdQuery(Guid userSyncId)
        {
            UserSyncId = userSyncId;
        }       
    }
}
