using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Exceptions
{
    public enum Error
    {
        Unauthorized = 1,
        InvalidCredentials=2,
        Forbidden = 3,
        CustomValidation = 4,
        NotFound = 5,
        Conflict = 6
    }
}
