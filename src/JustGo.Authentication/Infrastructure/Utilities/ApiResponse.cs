using System.Collections.Generic;
using System.Linq;

namespace JustGo.Authentication.Infrastructure.Utilities
{
    public class ApiResponse<TData, TPermissions>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public TData? Data { get; set; }
        public TPermissions? Permissions { get; set; }
        public List<string>? Errors { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorCode { get; set; }
        public string? TraceId { get; set; }

        public ApiResponse()
        {

        }

        public ApiResponse(TData? data, int statusCode = 200, string message = "Request successful", string errorCode = "")
        {
            Success = true;
            Message = message;
            Data = data;
            Errors = null;
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

        public ApiResponse(TData? data, TPermissions? permissions, int statusCode = 200, string message = "Request successful", string errorCode = "")
        {
            Success = true;
            Message = message;
            Data = data;
            Permissions = permissions;
            Errors = null;
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

        public ApiResponse(List<string>? errors, int statusCode = 500, string message = "An error occurred", string errorCode = "")
        {
            Success = false;
            Message = message;
            Data = default;
            Errors = errors;
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

        public static ApiResponse<TData, TPermissions> ErrorResult(string message, int statusCode,
            ICollection<string>? errors = null)
        {
            return new ApiResponse<TData, TPermissions>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors?.ToList(),
                StatusCode = statusCode,
                ErrorCode = "error"
            };
        }
    }
}
