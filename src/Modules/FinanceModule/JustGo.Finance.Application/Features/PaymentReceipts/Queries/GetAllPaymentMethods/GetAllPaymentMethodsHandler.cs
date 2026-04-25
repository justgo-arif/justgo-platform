using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAllPaymentMethods
{
    public class GetAllPaymentMethodsHandler : IRequestHandler<GetAllPaymentMethodsQuery, List<LookupStringDto>>
    {
        private readonly LazyService<IReadRepository<LookupStringDto>> _readRepository;

        public GetAllPaymentMethodsHandler(LazyService<IReadRepository<LookupStringDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<LookupStringDto>> Handle(GetAllPaymentMethodsQuery request, CancellationToken cancellationToken)
        {
            var sql = @"select distinct ISNULL(NULLIF(PaymentMethod,''),'N/A') as Id, ISNULL(NULLIF(PaymentMethod,''),'N/A') as Name from PaymentReceipts_default ";
            var paymentmethod = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, "text")).ToList();
            return paymentmethod;
        }
    }
}
