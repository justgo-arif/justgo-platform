#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Authentication.Infrastructure.Utilities;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        var errorResponse = ApiResponse<T, object>.ErrorResult(result.Error ?? "An error occurred",
            GetStatusCodeFromErrorType(result.ErrorType));
        
        return result.ErrorType switch
        {
            // 4xx Client Error Responses
            ErrorType.BadRequest => BadRequest(errorResponse),
            ErrorType.Unauthorized => Unauthorized(errorResponse),
            ErrorType.PaymentRequired => StatusCode(402, errorResponse),
            ErrorType.Forbidden => Forbid(),
            ErrorType.NotFound => NotFound(errorResponse),
            ErrorType.MethodNotAllowed => StatusCode(405, errorResponse),
            ErrorType.NotAcceptable => StatusCode(406, errorResponse),
            ErrorType.ProxyAuthenticationRequired => StatusCode(407, errorResponse),
            ErrorType.RequestTimeout => StatusCode(408, errorResponse),
            ErrorType.Conflict => Conflict(errorResponse),
            ErrorType.Gone => StatusCode(410, errorResponse),
            ErrorType.LengthRequired => StatusCode(411, errorResponse),
            ErrorType.PreconditionFailed => StatusCode(412, errorResponse),
            ErrorType.PayloadTooLarge => StatusCode(413, errorResponse),
            ErrorType.UriTooLong => StatusCode(414, errorResponse),
            ErrorType.UnsupportedMediaType => StatusCode(415, errorResponse),
            ErrorType.RangeNotSatisfiable => StatusCode(416, errorResponse),
            ErrorType.ExpectationFailed => StatusCode(417, errorResponse),
            ErrorType.ImATeapot => StatusCode(418, errorResponse),
            ErrorType.MisdirectedRequest => StatusCode(421, errorResponse),
            ErrorType.UnprocessableEntity => UnprocessableEntity(errorResponse),
            ErrorType.Locked => StatusCode(423, errorResponse),
            ErrorType.FailedDependency => StatusCode(424, errorResponse),
            ErrorType.TooEarly => StatusCode(425, errorResponse),
            ErrorType.UpgradeRequired => StatusCode(426, errorResponse),
            ErrorType.PreconditionRequired => StatusCode(428, errorResponse),
            ErrorType.TooManyRequests => StatusCode(429, errorResponse),
            ErrorType.RequestHeaderFieldsTooLarge => StatusCode(431, errorResponse),
            ErrorType.UnavailableForLegalReasons => StatusCode(451, errorResponse),

            // 5xx Server Error Responses
            ErrorType.InternalServerError => StatusCode(500, errorResponse),
            ErrorType.NotImplemented => StatusCode(501, errorResponse),
            ErrorType.BadGateway => StatusCode(502, errorResponse),
            ErrorType.ServiceUnavailable => StatusCode(503, errorResponse),
            ErrorType.GatewayTimeout => StatusCode(504, errorResponse),
            ErrorType.HttpVersionNotSupported => StatusCode(505, errorResponse),
            ErrorType.VariantAlsoNegotiates => StatusCode(506, errorResponse),
            ErrorType.InsufficientStorage => StatusCode(507, errorResponse),
            ErrorType.LoopDetected => StatusCode(508, errorResponse),
            ErrorType.NotExtended => StatusCode(510, errorResponse),
            ErrorType.NetworkAuthenticationRequired => StatusCode(511, errorResponse),

            // Custom validation error
            ErrorType.Validation => UnprocessableEntity(errorResponse),

            // Default fallback
            _ => StatusCode(500, errorResponse)
        };
    }

    private static int GetStatusCodeFromErrorType(ErrorType? errorType)
    {
        return errorType switch
        {
            // 4xx Client Error Responses
            ErrorType.BadRequest => 400,
            ErrorType.Unauthorized => 401,
            ErrorType.PaymentRequired => 402,
            ErrorType.Forbidden => 403,
            ErrorType.NotFound => 404,
            ErrorType.MethodNotAllowed => 405,
            ErrorType.NotAcceptable => 406,
            ErrorType.ProxyAuthenticationRequired => 407,
            ErrorType.RequestTimeout => 408,
            ErrorType.Conflict => 409,
            ErrorType.Gone => 410,
            ErrorType.LengthRequired => 411,
            ErrorType.PreconditionFailed => 412,
            ErrorType.PayloadTooLarge => 413,
            ErrorType.UriTooLong => 414,
            ErrorType.UnsupportedMediaType => 415,
            ErrorType.RangeNotSatisfiable => 416,
            ErrorType.ExpectationFailed => 417,
            ErrorType.ImATeapot => 418,
            ErrorType.MisdirectedRequest => 421,
            ErrorType.UnprocessableEntity => 422,
            ErrorType.Locked => 423,
            ErrorType.FailedDependency => 424,
            ErrorType.TooEarly => 425,
            ErrorType.UpgradeRequired => 426,
            ErrorType.PreconditionRequired => 428,
            ErrorType.TooManyRequests => 429,
            ErrorType.RequestHeaderFieldsTooLarge => 431,
            ErrorType.UnavailableForLegalReasons => 451,

            // 5xx Server Error Responses
            ErrorType.InternalServerError => 500,
            ErrorType.NotImplemented => 501,
            ErrorType.BadGateway => 502,
            ErrorType.ServiceUnavailable => 503,
            ErrorType.GatewayTimeout => 504,
            ErrorType.HttpVersionNotSupported => 505,
            ErrorType.VariantAlsoNegotiates => 506,
            ErrorType.InsufficientStorage => 507,
            ErrorType.LoopDetected => 508,
            ErrorType.NotExtended => 510,
            ErrorType.NetworkAuthenticationRequired => 511,

            // Custom validation error
            ErrorType.Validation => 422,

            // Default fallback
            _ => 500
        };
    }
}
#endif