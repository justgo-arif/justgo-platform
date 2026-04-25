using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.MemberPaymentDTOs;

namespace JustGo.Finance.Application.Features.Plans.Queries.GetPlanOwner
{
    public class GetPlanOwnerHandler : IRequestHandler<GetPlanOwnerQuery, PlanInfo>
    {
        private readonly LazyService<IReadRepository<PlanInfo>> _readRepository;

        public GetPlanOwnerHandler(LazyService<IReadRepository<PlanInfo>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<PlanInfo> Handle(GetPlanOwnerQuery request, CancellationToken cancellationToken)
        {
            var sql = @"SELECT  rpp.PlanGuid,rps.RecurringType,TRY_CAST(d.SyncGuid AS UNIQUEIDENTIFIER)  as MerchantId
                        FROM RecurringPaymentCustomer rc
                        INNER JOIN RecurringPaymentPlan rpp ON rc.Id = rpp.CustomerId
                        INNER JOIN RecurringPaymentScheme rps ON rps.Id = rpp.SchemeId
                        INNER JOIN Products_Default  pd ON pd.DocId = rpp.ProductId
                        Left Join Clubs_default cd on pd.Ownerid = cd.DocId
                        Left Join Document d on ISNULL(cd.DocId,27) = d.DocId
                        Where  rpp.PlanGuid =@PlanId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("PlanId", request.PlanId);

            var result = await _readRepository.Value
                .GetAsync(sql, cancellationToken, queryParameters, null, "text");
            return result;
        }
    }
}
