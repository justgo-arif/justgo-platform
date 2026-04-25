using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGoAPI.Shared.Helper
{
    public class OperationResult<T>:OperationResultSuccess
    {
        public T Data { get; set; }
        public int? TotalCount { get; set; } = 0;
        public int? NextId { get; set; } = 0;
    }
    public class OperationResultSuccess
    {
        public string Remark { get; set; } = "";
        public int StatusCode { get; set; }
        public string Message { get; set; } = "";
    }
}
