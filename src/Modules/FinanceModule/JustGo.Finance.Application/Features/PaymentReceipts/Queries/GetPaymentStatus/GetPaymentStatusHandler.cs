using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentStatus
{
    public class GetPaymentStatusHandler : IRequestHandler<GetPaymentStatusQuery, List<LookupIntDto>>
    {
        private readonly LazyService<IReadRepository<LookupIntDto>> _readRepository;

        public GetPaymentStatusHandler(LazyService<IReadRepository<LookupIntDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<LookupIntDto>> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
        {
            var sql = @"Select st.StateId as Id,Case When  st.name = 'PendingCustomerAuthorization' Then 'Pending Customer Authorization' Else st.name End  as Name from [State] st Where st.processid = 11";
            var payment = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, "text")).ToList();
            return payment;
        }
    }
}
