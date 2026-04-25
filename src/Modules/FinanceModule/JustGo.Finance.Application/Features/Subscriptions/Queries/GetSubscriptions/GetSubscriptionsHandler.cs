using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptions
{
    public class GetSubscriptionsHandler : IRequestHandler<GetSubscriptionsQuery, SubscriptionsVM>
    {
        private readonly LazyService<IReadRepository<SubscriptionsDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetSubscriptionsHandler(LazyService<IReadRepository<SubscriptionsDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<SubscriptionsVM> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
        {
            var resultVM = new SubscriptionsVM
            {
                PageNo = request.PageNo,
                PageSize = request.PageSize
            };

            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var queryParams = BuildParameters(request, ownerId);

            var sql = "[dbo].[GetSubscriptionInstallmentData]";
            await using var result = await _readRepository.Value.GetMultipleQueryAsync(sql, cancellationToken, queryParams);
            resultVM.TotalCount = (await result.ReadAsync<int>()).FirstOrDefault();
            resultVM.Subscriptions = (await result.ReadAsync<SubscriptionsDto>()).ToList();

            return resultVM;
        }
        private static DynamicParameters BuildParameters(GetSubscriptionsQuery request, int ownerId)
        {
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;
            if (string.IsNullOrWhiteSpace(request.ScopeKey) || request.ScopeKey == "string") request.ScopeKey = "all";

            var parameters = new DynamicParameters();

            parameters.Add("RecurringType", RecurringType.Subscription);
            parameters.Add("OwnerId", ownerId);

            var planIds = request.SubscriptionPlanIds?
                        .Where(x => Guid.TryParse(x, out _))
                        .Distinct()
                        .ToList() ?? new List<string>();

            parameters.Add("ProductId", string.Join(",", planIds));

            var validStatusIds = request.StatusIds?.Where(x => x > 0).ToList() ?? new();
            parameters.Add("StatusId", string.Join(",", validStatusIds));

            parameters.Add("PageNo", request.PageNo);
            parameters.Add("PageSize", request.PageSize);
            parameters.Add("ScopeKey", request.ScopeKey ?? string.Empty);
            parameters.Add("SearchText", request.SearchText ?? string.Empty);

            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                parameters.Add("DateUse", "Y");
                parameters.Add("FromDate", request.FromDate.Value.Date);
                parameters.Add("ToDate", request.ToDate.Value.Date.AddDays(1).AddTicks(-1));
            }
            else
            {
                parameters.Add("DateUse", "N");
                parameters.Add("FromDate", null);
                parameters.Add("ToDate", null);
            }
            parameters.Add("TotalCount", request.TotalCount);

            return parameters;
        }
    }
}
