using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Logging
{
    public class ExceptionLog
    {
        public long ExceptionLogId { get; set; }
        public string? TraceId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int UserId { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
