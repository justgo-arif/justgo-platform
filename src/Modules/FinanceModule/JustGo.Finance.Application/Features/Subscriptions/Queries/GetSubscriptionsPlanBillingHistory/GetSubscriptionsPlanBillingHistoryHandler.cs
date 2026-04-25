using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlanBillingHistory
{
    public class GetSubscriptionsPlanBillingHistoryHandler : IRequestHandler<GetSubscriptionsPlanBillingHistoryQuery, PaginatedResponse<RecurringPaymentHistory>>
    {
        private readonly LazyService<IReadRepository<RecurringPaymentHistory>> _readRepository;
        private readonly IMediator _mediator;

        public GetSubscriptionsPlanBillingHistoryHandler(LazyService<IReadRepository<RecurringPaymentHistory>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaginatedResponse<RecurringPaymentHistory>> Handle(GetSubscriptionsPlanBillingHistoryQuery request, CancellationToken cancellationToken)
        {
            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);

            var queryParameters = new DynamicParameters();
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;

            queryParameters.Add("RecurringType", RecurringType.Subscription);
            queryParameters.Add("OwnerId", ownerId);
            queryParameters.Add("PlanId", request.PlanId);
            queryParameters.Add("SearchText", request.SearchText ?? string.Empty);

            queryParameters.Add("PageNo", request.PageNo);
            queryParameters.Add("PageSize", request.PageSize);
            var columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    { "InvoiceId", "Paymentid" },
                                    { "BillingDate", "PaidDate" }
                                };

            var allowedOrders = new[] { "ASC", "DESC" };

            if (!string.IsNullOrEmpty(request?.ColumnName) &&
                columnMappings.ContainsKey(request.ColumnName) &&
                allowedOrders.Contains(request.OrderBy?.ToUpper()))
            {
                var backendColumn = columnMappings[request.ColumnName];
                queryParameters.Add("ColumnName", backendColumn);
                queryParameters.Add("OrderBy", request.OrderBy?.ToUpper());
            }
            else
            {
                queryParameters.Add("ColumnName", "Paymentid");
                queryParameters.Add("OrderBy", "ASC");
            }

            var sql = @"GetInstallmentSubscriptionHistory";
            await using var result = await _readRepository.Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters);
            var totalCount = (await result.ReadAsync<int>()).FirstOrDefault();
            var data = (await result.ReadAsync<RecurringPaymentHistory>()).ToList();
            return new PaginatedResponse<RecurringPaymentHistory>(data, request!.PageNo, request.PageSize, totalCount);
        }
    }
}
