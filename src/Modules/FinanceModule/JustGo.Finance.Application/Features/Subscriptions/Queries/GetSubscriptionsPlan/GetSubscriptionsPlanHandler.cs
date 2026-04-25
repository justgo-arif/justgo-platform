using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlan
{
    public class GetSubscriptionsPlanHandler : IRequestHandler<GetSubscriptionsPlanQuery, List<LookupStringDto>?>
    {
        private readonly LazyService<IReadRepository<LookupStringDto>> _readRepository;
        private readonly IMediator _mediator;
        private readonly ICustomError _error;

        public GetSubscriptionsPlanHandler(LazyService<IReadRepository<LookupStringDto>> readRepository, IMediator mediator, ICustomError error)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _error = error;
        }

        public async Task<List<LookupStringDto>?> Handle(GetSubscriptionsPlanQuery request, CancellationToken cancellationToken)
        {
            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var queryParams = new DynamicParameters();
            queryParams.Add("OwnerId", ownerId);
            queryParams.Add("RecurringType", RecurringType.Subscription);

            var sql = @"select Distinct pd.DocId,d.SyncGuid as Id,pd.Name from RecurringPaymentPlan rpp 
                        INNER JOIN RecurringPaymentScheme rps on rps.Id=rpp.SchemeId
                        inner join Products_Default pd on pd.docid=rpp.ProductId 
                        Inner Join Document d on pd.DocId = d.DocId
                        Where ( rps.RecurringType =@RecurringType OR rps.RecurringType = 3 ) AND ISNULL(pd.Ownerid,0) = @OwnerId
                        Order By pd.docid ASC";
            var list = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParams, null, "text")).ToList();

            if (list.Count == 0)
            {
                _error.NotFound<object>($"No data found for given criteria");
                return null;
            }

            return list;
        }
    }

}
