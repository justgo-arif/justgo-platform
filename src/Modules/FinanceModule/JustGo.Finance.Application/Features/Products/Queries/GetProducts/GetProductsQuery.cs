using JustGo.Finance.Application.DTOs.ProductDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Products.Queries.GetProducts
{
    public class GetProductsQuery : IRequest<ProductListResponse>
    {
        public Guid MerchantId { get; set; }
        public string? SearchText { get; set; }
        public int PageSize { get; set; }
        public long? LastSeenDocId { get; set; }
        public int? TotalCount { get; set; } 
    }

}
