using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.UserDeviceSessions.Queries.GetRefreshTokenExpiryDateByRefreshToken
{
    public class GetRefreshTokenExpiryDateByRefreshTokenQuery : IRequest<DateTime?>
    {
        public GetRefreshTokenExpiryDateByRefreshTokenQuery(string refreshToken)
        {
            RefreshToken = refreshToken;
        }

        public string RefreshToken { get; set; }
    }
}
