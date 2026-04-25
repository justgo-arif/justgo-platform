using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Users.Commands.RefreshTokens
{
    public class RefreshTokenCommand:IRequest<RefreshTokenResponse>
    {
    }
}
