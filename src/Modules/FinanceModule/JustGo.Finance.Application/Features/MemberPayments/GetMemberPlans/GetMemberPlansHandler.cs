using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.MemberPaymentDTOs;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPlans
{
    public class GetMemberPlansHandler : IRequestHandler<GetMemberPlansQuery, PlansPageVM>
    {
        private readonly LazyService<IReadRepository<Plan>> _readRepository;
        private readonly IMediator _mediator;

        public GetMemberPlansHandler(LazyService<IReadRepository<Plan>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PlansPageVM> Handle(GetMemberPlansQuery request, CancellationToken cancellationToken)
        {
            var resultVM = new PlansPageVM();

            var memberdocid = await _mediator.Send(
                new GetDocIdBySyncGuidQuery(request.MemberId), cancellationToken);

            var queryParams = BuildParameters(memberdocid,request.Status);

            var sql = "[dbo].[GetUserPlanData]";

            var plans = (await _readRepository.Value
                .GetListAsync(sql, cancellationToken, queryParams))
                .ToList();
             
            resultVM.Categories = plans
                .GroupBy(p => p.Category)
                .Select(g => new PlanCategory
                {
                    Name = g.Key,
                    Plans = g.ToList()
                })
                .ToList();

            return resultVM;
        }

        private static DynamicParameters BuildParameters(int memberdocid, string? status)
        {
            var parameters = new DynamicParameters();
            parameters.Add("MemberDocId", memberdocid);

            int statusValue = (int)PlanStatus.Active;  

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<PlanStatus>(status, true, out var parsedStatus))
            {
                statusValue = (int)parsedStatus;
            }

            parameters.Add("Status", statusValue);

            return parameters;
        }

    }
}
