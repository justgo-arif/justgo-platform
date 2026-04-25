using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.CustomErrors
{
    public interface IShortCircuitResponder
    {
        void SetResponse(IShortCircuitResponse response);
    }
}
