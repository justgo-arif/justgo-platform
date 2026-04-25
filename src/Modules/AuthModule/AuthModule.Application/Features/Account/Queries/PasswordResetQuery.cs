using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Account.Queries
{
    public class PasswordResetQuery:IRequest<Tuple<bool, string>>
    {
        public string OrganizationId { get; set; }
        public string UserId { get; set; }  
    }
}
