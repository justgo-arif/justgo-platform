using System.Data;
using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDropdown
{
    public class GetPricingChartDropdownHandler : IRequestHandler<GetPricingChartDropdownQuery, List<PricingChartDropdownDto>>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetPricingChartDropdownHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<PricingChartDropdownDto>> Handle(GetPricingChartDropdownQuery request, CancellationToken cancellationToken = default)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", request.SearchTerm);
            parameters.Add("@OwnerGuid", request.OwnerGuid.ToString(), DbType.String, size: 100);
            parameters.Add("@PageNumber", 1);
            parameters.Add("@PageSize", 100);
            parameters.Add("@SortColumn", "Name");
            parameters.Add("@SortDirection", "ASC");
            parameters.Add("@IsActiveOnly", 1);

            var result = await _readRepository
                .GetLazyRepository<PricingChartDropdownDto>()
                .Value
                .GetListAsync(
                    "JustGoBookingClassPricingChart_GetList",
                    cancellationToken,
                    parameters,
                    null,
                    "sp"
                );

            return result?.ToList() ?? new List<PricingChartDropdownDto>();
        }
    }
}
