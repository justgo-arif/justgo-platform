using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentUpcomingSchedule
{
    public class GetInstallmentUpcomingScheduleHandler : IRequestHandler<GetInstallmentUpcomingScheduleQuery, PaginatedResponse<RecurringPaymentScheduleDto>>
    {
        private readonly LazyService<IReadRepository<RecurringPaymentScheduleDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetInstallmentUpcomingScheduleHandler(LazyService<IReadRepository<RecurringPaymentScheduleDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaginatedResponse<RecurringPaymentScheduleDto>> Handle(GetInstallmentUpcomingScheduleQuery request, CancellationToken cancellationToken)
        {

            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var queryParameters = new DynamicParameters();

            queryParameters.Add("RecurringType", RecurringType.Installment);
            queryParameters.Add("OwnerId", ownerId);
            queryParameters.Add("PlanId", request.PlanId);
            queryParameters.Add("SearchText", request.SearchText ?? string.Empty);

            queryParameters.Add("PageNo", request.PageNo);
            queryParameters.Add("PageSize", request.PageSize);
            var columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    { "SchemeNo", "SchemeNo" },
                                    { "DueDate", "PaymentDate" }
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
                queryParameters.Add("ColumnName", "SchemeNo");
                queryParameters.Add("OrderBy", "ASC");
            }

            var sql = @"GetInstallmentUpcomingSchedule";
            await using var result = await _readRepository.Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters);
            var totalCount = (await result.ReadAsync<int>()).FirstOrDefault();
            var data = (await result.ReadAsync<RecurringPaymentScheduleDto>()).ToList();
            return new PaginatedResponse<RecurringPaymentScheduleDto>(data, request!.PageNo, request.PageSize, totalCount);
        }
    }
}
