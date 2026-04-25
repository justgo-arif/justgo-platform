using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetTimeZoneValue
{
    public class IsEnableMFAQuery : IRequest<MFACommonResponse>
    {
        public string UserName { get; set; }
        public string DeviceIdentifier { get; set; }    
        
    }
}
