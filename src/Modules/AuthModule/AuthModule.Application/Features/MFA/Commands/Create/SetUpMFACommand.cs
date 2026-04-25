using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Commands.Create
{
    public class SetUpMFACommand:IRequest<bool>
    {
        public int UserId { get; set; }
        public string AuthChannel { get; set; } 
        public UserMFA UserMFA { get; set; }
    }
}
