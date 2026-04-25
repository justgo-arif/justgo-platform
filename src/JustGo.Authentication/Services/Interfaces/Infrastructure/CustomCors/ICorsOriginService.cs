using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.CustomCors
{
    public interface ICorsOriginService
    {
#if NET9_0_OR_GREATER
        List<string> GetAllowedOrigins();
        List<string> GetAllowedOriginsByOrigin(string origin);
        bool IsTenantOriginAllowed(string origin);
#endif
    }
}
