using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDropdown
{
    public class GetPricingChartDropdownQuery : IRequest<List<PricingChartDropdownDto>>
    {
        public string OwnerGuid { get; }
        public string? SearchTerm { get; }

        public GetPricingChartDropdownQuery(string ownerGuid, string? searchTerm)
        {
            OwnerGuid = ownerGuid;
            SearchTerm = searchTerm;
        }
    }
}
