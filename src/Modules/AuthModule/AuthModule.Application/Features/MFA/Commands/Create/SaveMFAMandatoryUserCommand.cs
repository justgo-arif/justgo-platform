using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Commands.Create
{
    public class SaveMFAMandatoryUserCommand:IRequest<bool>
    {
        public int UserId { get; set; }
        public bool UpdateFlag { get; set; }
    }
}
