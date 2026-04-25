namespace JustGo.Authentication.Infrastructure.Utilities
{
    public class ApiResponseWithCount<T, TPermissions> : ApiResponse<T, TPermissions>
    {
        public int TotalCount { get; set; }
        public int NextId { get; set; }

        public ApiResponseWithCount(
            T data,
            TPermissions permissions,
            int totalCount,
            int nextId,
            int statusCode = 200,
            string message = "Request successful",
            string errorCode = ""
        ) : base(data, statusCode, message, errorCode)
        {
            TotalCount = totalCount;
            NextId = nextId;
        }
    }
}
