using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlan
{
    public class GetInstallmentPlanNameHandler : IRequestHandler<GetInstallmentPlanNameQuery, List<LookupStringDto>>
    {
        private readonly LazyService<IReadRepository<LookupStringDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetInstallmentPlanNameHandler(LazyService<IReadRepository<LookupStringDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<LookupStringDto>> Handle(GetInstallmentPlanNameQuery request, CancellationToken cancellationToken)
        {
            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var queryParams = new DynamicParameters();
            queryParams.Add("OwnerId", ownerId);
            queryParams.Add("RecurringType", RecurringType.Installment);
            var sql = @"select Distinct pd.DocId,d.SyncGuid as Id,pd.Name from RecurringPaymentPlan rpp 
                        INNER JOIN RecurringPaymentScheme rps on rps.Id=rpp.SchemeId
                        inner join Products_Default pd on pd.docid=rpp.ProductId 
                        Inner Join Document d on pd.DocId = d.DocId
                        Where rps.RecurringType =@RecurringType AND ISNULL(pd.Ownerid,0) = @OwnerId
                        Order By pd.docid ASC";
            return (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParams, null, "text")).ToList();
        }
    }
}
