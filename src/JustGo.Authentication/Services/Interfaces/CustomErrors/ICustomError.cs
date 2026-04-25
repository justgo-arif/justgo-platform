using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Authentication.Services.Interfaces.CustomErrors
{
    public interface ICustomError
    {
#if NET9_0_OR_GREATER       
        T Unauthorized<T>(string message);
        T InvalidCredentials<T>(string message);
        T Forbidden<T>(string message);
        T CustomValidation<T>(string message);
        T NotFound<T>(string message);
        T Conflict<T>(string message);
#endif
    }
}
