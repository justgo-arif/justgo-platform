using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDiscountList
{

    public class GetPricingChartDiscountListQuery : IRequest<List<PricingChartDiscountListDto>>
    {
        public string? SearchTerm { get; }
        public  int OwnerId { get; }
        public GetPricingChartDiscountListQuery(string? searchTerm, int ownerId)
        {
            SearchTerm = searchTerm;
            OwnerId = ownerId;
        }
    }
}
