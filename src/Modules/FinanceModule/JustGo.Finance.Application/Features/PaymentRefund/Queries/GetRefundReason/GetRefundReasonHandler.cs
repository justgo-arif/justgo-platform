using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundReason
{

    public class GetRefundReasonHandler : IRequestHandler<GetRefundReasonQuery, List<LookupIntDto>>
    {
        private readonly LazyService<IReadRepository<LookupIntDto>> _readRepository;

        public GetRefundReasonHandler(LazyService<IReadRepository<LookupIntDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<LookupIntDto>> Handle(GetRefundReasonQuery request, CancellationToken cancellationToken)
        {
            var sql = @"SELECT 
                        v.Id,
                        CASE v.Id
                        WHEN 1 THEN 'Duplicate'
                        WHEN 2 THEN 'Fraudulent'
                        WHEN 3 THEN 'Request by customer'
                        ELSE 'Others'
                        END AS Name
                        FROM (VALUES (1), (2), (3),(4)) AS v(Id);";
            return (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, "text")).ToList();
        }
    }

}
