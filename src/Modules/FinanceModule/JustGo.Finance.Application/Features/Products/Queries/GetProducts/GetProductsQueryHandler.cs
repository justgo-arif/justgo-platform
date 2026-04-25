using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.ProductDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Products.Queries.GetProducts
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ProductListResponse>
    {
        private readonly LazyService<IReadRepository<ProductDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetProductsQueryHandler(LazyService<IReadRepository<ProductDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<ProductListResponse> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var productListResponse = new ProductListResponse();
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;

            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var queryParams = new
            {
                PageSize = request.PageSize + 1,
                OwnerId = ownerId,
                request.SearchText,
                request.LastSeenDocId
            };

            var sql = @$"
                        SELECT TOP (@PageSize)
                            CAST(d.SyncGuid AS UNIQUEIDENTIFIER) AS Id,
                            pd.DocId,
                            pd.Code,
                            pd.Name,
                            pd.Description,
                            pd.Category,
                            pd.UnitPrice,
                            pd.Currency,
                            pd.OwnerId,
                            pd.ProductReference,
                            ISNULL(pd.Location, '') AS ProductImageURL
                        FROM Products_Default pd
                        INNER JOIN Document d 
                            ON pd.DocId = d.DocId
                        WHERE 
                            pd.OwnerId = @OwnerId
                            AND (@LastSeenDocId IS NULL OR pd.DocId > @LastSeenDocId)
                            AND (
                                @SearchText IS NULL
                                OR pd.Name LIKE '%' + @SearchText + '%'
                                OR pd.Description LIKE '%' + @SearchText + '%'
                             )
                        ORDER BY pd.DocId ASC;";
            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParams, null, "text")).ToList();

            var hasMore = result.Count > request.PageSize;
            var finalItems = hasMore ? result.Take(request.PageSize).ToList() : result;
            if (request.TotalCount == null || request.TotalCount == 0)
            {
                var sqlCount = @$"
                        SELECT COUNT(pd.DocId)
                        FROM Products_Default pd
                        WHERE 
                            pd.OwnerId = @OwnerId 
                            AND (
                                @SearchText IS NULL
                                OR pd.Name LIKE '%' + @SearchText + '%'
                                OR pd.Description LIKE '%' + @SearchText + '%'
                             ) ";
                var totalcountobj = await _readRepository.Value.GetSingleAsync(sqlCount, cancellationToken, queryParams, null, "text");
                productListResponse.TotalCount = totalcountobj != null ? Convert.ToInt32(totalcountobj) : 0;
            }
            else
            {
                productListResponse.TotalCount = request.TotalCount;
            }
            productListResponse.Items = finalItems;
            productListResponse.PageSize = request.PageSize;
            productListResponse.NextLastSeenDocId = hasMore ? finalItems[^1].DocId : null;
            return productListResponse;
        }
    }

}
