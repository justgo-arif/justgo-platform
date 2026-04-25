using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentConsoleProduct
{
    public class GetPaymentConsoleProductQueryHandler : IRequestHandler<GetPaymentConsoleProductQuery, List<OwnerWiseProductDto>>
    {
        private readonly LazyService<IReadRepository<OwnerWiseProductDto>> _readRepository;
        public GetPaymentConsoleProductQueryHandler(LazyService<IReadRepository<OwnerWiseProductDto>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<OwnerWiseProductDto>> Handle(GetPaymentConsoleProductQuery request, CancellationToken cancellationToken)
        {
            var query = @"select  pcp.ProductId,	pcp.CategoryId,	pcp.OwnerId, pcpc.CategoryName 
from PaymentConsoleProducts pcp 
INNER JOIN PaymentConsoleProductCategory pcpc on pcp.CategoryId = pcpc.ConsoleProductCategoryId
WHERE OwnerId =   @OwnerId  
                          ORDER BY ConsoleProductId DESC";

            var parameters = new
            {
                OwnerId = request.OwnerId 
            };

            return (await _readRepository.Value.GetListAsync(query, cancellationToken, parameters, null, "text")).ToList();
            
        }
    }
}
