using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomErrors;

namespace JustGo.Authentication.Infrastructure.CustomErrors
{
    public class ShortCircuitResponse : IShortCircuitResponse
    {
        public int StatusCode { get; }
        public object ResponseBody { get; }

        public ShortCircuitResponse(int statusCode, object body)
        {
            StatusCode = statusCode;
            ResponseBody = body;
        }
    }
}
