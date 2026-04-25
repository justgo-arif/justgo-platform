using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif

namespace JustGo.Authentication.Infrastructure.CustomErrors
{
#if NET9_0_OR_GREATER
    public class ShortCircuitResponder : IShortCircuitResponder
    {

        private readonly IHttpContextAccessor _httpContextAccessor;

        public ShortCircuitResponder(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetResponse(IShortCircuitResponse response)
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No active HTTP context.");

            httpContext.Items["ShortCircuitResponse"] = response;
        }

    }
#endif
}
