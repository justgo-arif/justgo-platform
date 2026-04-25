using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs.ProductDTOs
{
    public class ProductListResponse 
    {
        public IEnumerable<ProductDto>? Items { get; set; }
        public int? TotalCount { get; set; }
        public int PageSize { get; set; }
        public long? NextLastSeenDocId { get; set; }
        public bool HasMore => NextLastSeenDocId != null;
    }

}
