using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Balances;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAdyenBalanceAccounts;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Components.Forms;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAdyenPayouts
{
    public class GetAdyenPayoutsHandler : IRequestHandler<GetAdyenPayoutsQuery, AdyenPayoutInfoVM>
    {
        private readonly LazyService<IReadRepository<AdyenPayoutInfoDTO>> _readRepository;
        private readonly IMediator _mediator;
        public GetAdyenPayoutsHandler(
            LazyService<IReadRepository<AdyenPayoutInfoDTO>> readRepository, 
            IMediator mediator
            )
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<AdyenPayoutInfoVM> Handle(GetAdyenPayoutsQuery request, CancellationToken cancellationToken)
        {
            var adyenPayoutInfoVM = new AdyenPayoutInfoVM();
            var balanceAccountsQuery = new GetAdyenBalanceAccountsQuery(request.MerchantId);
            var balanceAccounts = await _mediator.Send(balanceAccountsQuery, cancellationToken);

            if (!balanceAccounts.Any())
                throw new NotFoundException("Balance Account not Found.");

            var _balanceAccount = balanceAccounts.First();

            var queryParameters = new DynamicParameters();
            queryParameters.Add("balanceAccountId", _balanceAccount.BalanceAccountId);

            string filterClause = "WHERE BalanceAccount = @balanceAccountId";

            if (request.FromDate.HasValue)
            {
                filterClause += " AND Initiated >= @startDate";
                queryParameters.Add("startDate", request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                filterClause += " AND Initiated <= @endDate";
                queryParameters.Add("endDate", request.ToDate.Value);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                filterClause += " AND [Status] = @status";
                queryParameters.Add("status", request.Status);
            }

            // Total count using GetSingleAsync
            string countQuery = $"SELECT COUNT(*) FROM AdyenPayouts {filterClause}";
            var countResult = await _readRepository.Value.GetSingleAsync(
                countQuery, queryParameters, null, "text");

            adyenPayoutInfoVM.TotalCount = Convert.ToInt32(countResult);

            // Pagination
            int offset = (request.PageNo - 1) * request.PageSize;
            queryParameters.Add("offset", offset);
            queryParameters.Add("pageSize", request.PageSize);

            // Safe sorting
            string sortColumn = (!string.IsNullOrEmpty(request.OrderBy) &&
                                 (request.OrderBy.Equals("Initiated", StringComparison.OrdinalIgnoreCase) ||
                                  request.OrderBy.Equals("EstimatedArrival", StringComparison.OrdinalIgnoreCase)))
                                ? request.OrderBy
                                : "Initiated";

            string sortDirection = request.SortDirection?.Equals("DESC", StringComparison.OrdinalIgnoreCase) == true
                ? "DESC" : "ASC";

            // Final paginated query
            var dataQuery = $@"
                SELECT 
                    PayoutReferenceId,
                    BalanceAccount,
                    TransferId,
                    (Amount / 100) AS Amount,
                    Currency,
                    [Status],
                    ExternalAccount,
                    [Description],
                    Initiated,
                    EstimatedArrival
                FROM AdyenPayouts
                {filterClause}
                ORDER BY {sortColumn} {sortDirection}
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
            ";

            var adyenPayouts = await _readRepository.Value.GetListAsync(
                dataQuery, cancellationToken, queryParameters, null, "text");

            adyenPayoutInfoVM.AdyenPayoutInfos = adyenPayouts.ToList();
            adyenPayoutInfoVM.PageNo = request.PageNo;
            adyenPayoutInfoVM.PageSize = request.PageSize;

            return adyenPayoutInfoVM;
        }


    }
    
}
