using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif

namespace JustGo.Authentication.Infrastructure.CustomErrors
{
    public static class CustomResponse
    {
#if NET9_0_OR_GREATER
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static bool IsFailure =>
            _httpContextAccessor?.HttpContext?.Items.ContainsKey("ShortCircuitResponse") == true;
#endif
    }
}
