using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDiscountList
{
    public class GetPricingChartDiscountListHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<GetPricingChartDiscountListQuery, List<PricingChartDiscountListDto>>
    {
        public async Task<List<PricingChartDiscountListDto>> Handle(GetPricingChartDiscountListQuery request, CancellationToken cancellationToken = default)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", request.SearchTerm);
            parameters.Add("@OwnerId", request.OwnerId);

            const string sql = """
                                SELECT 
                                   CD.PricingChartDiscountId, 
                                   CD.PricingChartDiscountName, 
                                   CD.PricingChartDiscountType, 
                                   CD.PricingChartDiscountValue, 
                                   CD.PricingChartDiscountStatus, 
                                   CD.CreatedDate, 
                                   CD.CreatedBy, 
                                   CD.IsDeleted,
                                   CDD.PricingChartId,
                                   PC.Name
                               FROM JustGoBookingClassPricingChartDiscount CD
                               INNER JOIN JustGoBookingClassPricingChartDiscountDetails CDD 
                                   ON CD.PricingChartDiscountId = CDD.PricingChartDiscountId
                               INNER JOIN JustGoBookingClassPricingChart PC 
                                   ON CDD.PricingChartId = PC.PricingChartId
                               WHERE 
                               PC.OwnerId = @OwnerId
                               AND CD.IsDeleted = 0
                               AND PC.IsDeleted = 0
                               AND CDD.IsDeleted = 0
                               AND (@SearchTerm IS NULL OR CD.PricingChartDiscountName LIKE '%' + @SearchTerm + '%')
                               ORDER BY CD.CreatedDate DESC
                               """;

            var result = await readRepository
                .GetLazyRepository<PricingChartDiscountListDto>()
                .Value
                .GetListAsync(sql, cancellationToken, parameters, null, "text");

            var grouped = result?
                .GroupBy(x => x.PricingChartDiscountId)
                .Select(g => new PricingChartDiscountListDto
                {
                    PricingChartDiscountId = g.Key,
                    PricingChartDiscountName = g.First().PricingChartDiscountName,
                    PricingChartDiscountType = g.First().PricingChartDiscountType,
                    PricingChartDiscountValue = g.First().PricingChartDiscountValue,
                    PricingChartDiscountStatus = g.First().PricingChartDiscountStatus,
                    CreatedDate = g.First().CreatedDate,
                    CreatedBy = g.First().CreatedBy,
                    IsDeleted = g.First().IsDeleted,
                    PricingChartIds = g.Select(x => x.PricingChartId).Distinct().ToList(),
                    PricingChartNames = g.Select(x => x.Name).Distinct().ToList()
                })
                .ToList() ?? [];

            return grouped;
        }
    }
}
