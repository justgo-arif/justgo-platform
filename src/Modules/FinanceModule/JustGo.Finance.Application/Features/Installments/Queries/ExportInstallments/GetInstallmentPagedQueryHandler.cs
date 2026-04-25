using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Installments.Queries.ExportInstallments
{
    public class GetInstallmentPagedQueryHandler : IRequestHandler<GetInstallmentPagedQuery, List<InstallmentDto>>
    {
        private readonly LazyService<IReadRepository<InstallmentDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetInstallmentPagedQueryHandler(LazyService<IReadRepository<InstallmentDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<InstallmentDto>> Handle(GetInstallmentPagedQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string")
                request.SearchText = null;

            if (request.InstallmentPlanIds?.Count == 1 && request.InstallmentPlanIds[0] == "string")
                request.InstallmentPlanIds.Clear();

            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken); 
            var queryParams = BuildParameters(request, ownerId);

            var sql = "[dbo].[GetSubscriptionInstallmentExportData]";
            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParams)).ToList();

            return result?.ToList() ?? new List<InstallmentDto>();
        }
        private static DynamicParameters BuildParameters(GetInstallmentPagedQuery request, int ownerId)
        {
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;
            if (string.IsNullOrWhiteSpace(request.ScopeKey) || request.ScopeKey == "string") request.ScopeKey = "all";
            var parameters = new DynamicParameters();

            parameters.Add("RecurringType", request.RecurringType);
            parameters.Add("OwnerId", ownerId);

            var planIds = request.InstallmentPlanIds?
                        .Where(x => Guid.TryParse(x, out _))
                        .Distinct()
                        .ToList() ?? new List<string>();

            parameters.Add("ProductId", string.Join(",", planIds));

            var validStatusIds = request.StatusIds?.Where(x => x > 0).ToList() ?? new();
            parameters.Add("StatusId", string.Join(",", validStatusIds));

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
            parameters.Add("ScopeKey", request.ScopeKey ?? string.Empty);
            parameters.Add("SearchText", request.SearchText ?? string.Empty);
            parameters.Add("PageNo", request.PageNo);
            parameters.Add("PageSize", request.PageSize);
            return parameters;
        }
    }
}
