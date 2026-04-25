using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Users.Queries.GetUserByLoginId
{
    public class GetUserByLoginIdQuery:IRequest<User>
    {
        public string LoginId { get; set; }
        public GetUserByLoginIdQuery(string loginId)
        {
            LoginId=loginId;
        }       
    }
}
