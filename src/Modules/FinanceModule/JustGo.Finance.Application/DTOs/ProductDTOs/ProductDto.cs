using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.ProductDTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public int DocId { get; set; }
        public string? Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? Category { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; } 
        public string Currency { get; set; } = string.Empty;
        public decimal OwnerId { get; set; }
        public string? ProductReference { get; set; }
        public string? ProductImageURL { get; set; }
    }

}
