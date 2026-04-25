using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif

namespace JustGo.Authentication.Infrastructure.CustomErrors
{
    public class CustomError : ICustomError
    {
#if NET9_0_OR_GREATER
        ApiResponse<string,object> response;
        int statusCode = StatusCodes.Status500InternalServerError;
        string defaultMessage = "An unexpected error occurred";
        string errorCode = "unexpected_error";
        private readonly IShortCircuitResponder _shortCircuitResponder;

        public CustomError(IShortCircuitResponder shortCircuitResponder)
        {
            _shortCircuitResponder = shortCircuitResponder;
        }

        public T Conflict<T>(string message)
        {
            statusCode = StatusCodes.Status409Conflict;
            defaultMessage = "Record can't be duplicated";
            errorCode = "resource_conflict";
            response = new ApiResponse<string, object>(new List<string> { message }, statusCode, defaultMessage, errorCode);
            _shortCircuitResponder.SetResponse(new ShortCircuitResponse(
                StatusCodes.Status409Conflict,
                response
                ));
            return default;
        }

        public T CustomValidation<T>(string message)
        {
            statusCode = StatusCodes.Status400BadRequest;
            defaultMessage = "Validation error";
            errorCode = "custom_validation_failed";
            response = new ApiResponse<string, object>(new List<string> { message }, statusCode, defaultMessage, errorCode);
            _shortCircuitResponder.SetResponse(new ShortCircuitResponse(
               statusCode,
               response
               ));
            return default;
        }

        public T Forbidden<T>(string message)
        {
            statusCode = StatusCodes.Status403Forbidden;
            defaultMessage = "Access is forbidden";
            errorCode = "forbidden_access";
            response = new ApiResponse<string, object>(new List<string> { message }, statusCode, defaultMessage, errorCode);
            _shortCircuitResponder.SetResponse(new ShortCircuitResponse(
               statusCode,
               response
               ));
            return default;
        }

        public T InvalidCredentials<T>(string message)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            defaultMessage = "Invalid credentials";
            errorCode = "invalid_credentials";
            response = new ApiResponse<string, object>(new List<string> { message }, statusCode, defaultMessage, errorCode);
            _shortCircuitResponder.SetResponse(new ShortCircuitResponse(
               statusCode,
               response
               ));
            return default;
        }

        public T NotFound<T>(string message)
        {
            statusCode = StatusCodes.Status404NotFound;
            defaultMessage = "No data found";
            errorCode = "resource_not_found";
            response = new ApiResponse<string, object>(new List<string> { message }, statusCode, defaultMessage, errorCode);
            _shortCircuitResponder.SetResponse(new ShortCircuitResponse(
               statusCode,
               response
               ));
            return default;
        }

        public T Unauthorized<T>(string message)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            defaultMessage = "Unauthorized";
            errorCode = "unauthorized_access";
            response = new ApiResponse<string, object>(new List<string> { message }, statusCode, defaultMessage, errorCode);
            _shortCircuitResponder.SetResponse(new ShortCircuitResponse(
               statusCode,
               response
               ));
            return default;
        }
#endif
    }
}
