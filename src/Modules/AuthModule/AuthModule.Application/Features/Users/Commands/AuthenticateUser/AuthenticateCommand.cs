using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Users.Commands.AuthenticateUser
{
    public class AuthenticateCommand:IRequest<AuthenticateResponse>
    {
        //public string TenantId { get; set; } = "";
        public string TenantClientId { get; set; }
        public string LoginId { get; set; }
        public string Password { get; set; }
    }
}
