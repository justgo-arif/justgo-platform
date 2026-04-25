using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Result.Domain.Entities
{
    public class ResultUploadFieldMapping
    {
        public required string ColumnName { get; set; }
        public bool IsOptional { get; set; }
        public string SampleData { get; set; } = string.Empty;
        public required int ColumnIdentifier { get; set; }
    }
}
