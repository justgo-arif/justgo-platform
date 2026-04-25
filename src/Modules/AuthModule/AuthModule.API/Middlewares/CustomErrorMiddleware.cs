using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using Microsoft.AspNetCore.Http;

namespace AuthModule.API.Middlewares
{
    public class CustomErrorMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomErrorMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
           
            if (context.Items.TryGetValue("ShortCircuitResponse", out var obj) &&
            obj is IShortCircuitResponse shortCircuit)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = shortCircuit.StatusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(shortCircuit.ResponseBody);
                }
            }
        }
    }
}
