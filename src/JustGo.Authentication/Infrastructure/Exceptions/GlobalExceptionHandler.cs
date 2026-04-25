using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
#endif
using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Authentication.Infrastructure.Exceptions
{
#if NET9_0_OR_GREATER
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment _environment, IUtilityService _utilityService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            httpContext.Response.ContentType = "application/json";

            List<string> errors;
            var statusCode = StatusCodes.Status500InternalServerError;
            var message = "An unexpected error occurred";
            var errorCode = "unexpected_error";

            switch (exception)
            {
                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    message = "Unauthorized";
                    errorCode = "unauthorized_access";
                    errors = new List<string> { exception.Message };
                    break;

                case ForbiddenAccessException:
                    statusCode = StatusCodes.Status403Forbidden;
                    message = "Access is forbidden";
                    errorCode = "forbidden_access";
                    errors = new List<string> { exception.Message };
                    break;

                case SecurityTokenExpiredException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    message = "Unauthorized";
                    errorCode = "token_expired";
                    errors = new List<string> { "Your session has expired. Please login again." };
                    break;

                case FluentValidation.ValidationException fluentException:
                    statusCode = StatusCodes.Status400BadRequest;
                    message = "Validation error";
                    errorCode = "validation_failed";
                    errors = fluentException.Errors.Select(e => e.ErrorMessage).ToList();
                    break;

                case CustomValidationException:
                    statusCode = StatusCodes.Status400BadRequest;
                    message = "Validation error";
                    errorCode = "custom_validation_failed";
                    errors = new List<string> { exception.Message };
                    break;

                case NotFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    message = "No data found";
                    errorCode = "resource_not_found";
                    errors = new List<string> { exception.Message };
                    break;

                case TimeoutException:
                    statusCode = StatusCodes.Status408RequestTimeout;
                    message = "Request time out";
                    errorCode = "request_timeout";
                    errors = new List<string> { "The request took too long to process. Please try again." };
                    break;

                case OperationCanceledException:
                    statusCode = StatusCodes.Status408RequestTimeout;
                    message = "Request was canceled by the client";
                    errorCode = "operation_canceled";
                    errors = new List<string> { "The operation was canceled. Please see the log." };
                    break;

                case NullReferenceException:
                    statusCode = StatusCodes.Status500InternalServerError;
                    message = "Null reference";
                    errorCode = "null_reference";
                    errors = new List<string> { "An internal error occurred. Please see the log." };
                    break;

                case SqlException:
                    statusCode = StatusCodes.Status500InternalServerError;
                    message = "Database error occurred";
                    errorCode = "database_error";
                    errors = new List<string> { "A database error occurred. Please see the log." };
                    break;

                case InvalidCredentialsException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    message = "Invalid credentials";
                    errorCode = "invalid_credentials";
                    errors = new List<string> { exception.Message };
                    break;

                case ConflictException:
                    statusCode = StatusCodes.Status409Conflict;
                    message = "Record can't be duplicated";
                    errorCode = "resource_conflict";
                    errors = new List<string> { exception.Message };
                    break;

                case InvalidOperationException:
                    statusCode = StatusCodes.Status500InternalServerError;
                    message = "Invalid operation";
                    errorCode = "invalid_operation";
                    errors = new List<string> { "An invalid operation was attempted. Please see the log." };
                    break;

                case ArgumentNullException:
                case ArgumentException:                
                    statusCode = StatusCodes.Status400BadRequest;
                    message = "Bad request";
                    errorCode = "bad_request";
                    errors = new List<string> { "The request contains invalid data. Please see the log." };
                    break;

                default:
                    errors = new List<string> { "An unexpected error occurred. Please see the log." };
                    break;
            }


            //logger.LogError(exception, "Error occurred", response);
            string exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            int userId = 0;
            string ipAddress = "Unknown";
            try
            {
                userId = await _utilityService.GetCurrentUserId(cancellationToken);
            }
            catch (Exception){}
            try
            {
                ipAddress = _utilityService.GetClientIpAddress(new HttpContextAccessor { HttpContext = httpContext });
            }
            catch (Exception) { }
            string userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
            CustomLog.Exception(traceId, exceptionType, exception.Message, exception.StackTrace, statusCode, errorCode, message, userId, ipAddress, userAgent);

            if (_environment.IsDevelopment())
            {
                Console.WriteLine($"[DEV] TraceId: {traceId}");
                Console.WriteLine($"[DEV] Exception: {exception.GetType().Name}");
                Console.WriteLine($"[DEV] Message: {exception.Message}");
                Console.WriteLine($"[DEV] StackTrace: {exception.StackTrace}");
            }

            httpContext.Response.StatusCode = statusCode;
            var response = new ApiResponse<string, object>(errors, statusCode, message, errorCode);
            response.TraceId = traceId;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
#endif
}
