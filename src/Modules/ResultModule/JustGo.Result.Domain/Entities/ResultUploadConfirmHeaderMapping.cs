using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Result.Domain.Entities
{
    public  class ResultUploadConfirmHeaderMapping
    {
        public int ConfirmHeaderMappingId { get; set; }
        public int UploadedFileId { get; set; }
        public string SystemHeaderName { get; set; } = string.Empty;
        public string MappedHeaderName { get; set; } = string.Empty;
        public bool IsMapped { get; set; } = false;

    }
}