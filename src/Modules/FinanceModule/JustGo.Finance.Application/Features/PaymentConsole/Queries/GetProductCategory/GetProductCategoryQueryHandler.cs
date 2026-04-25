using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetProductCategory
{
    public class GetProductCategoryQueryHandler : IRequestHandler<GetProductCategoryQuery, List<LookupIntDto>>
    {
        private readonly LazyService<IReadRepository<LookupIntDto>> _readRepository;

        public GetProductCategoryQueryHandler(LazyService<IReadRepository<LookupIntDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<LookupIntDto>> Handle(GetProductCategoryQuery request, CancellationToken cancellationToken)
        {

            var sql = @"select ConsoleProductCategoryId as Id,CategoryName as Name from PaymentConsoleProductCategory";
            return (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, "text")).ToList();
        }
    }
}
