using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.ClassManagementDtos;

namespace JustGo.Booking.Application.Features.ClassManagement.Queries.ProRataCalculation
{
    public class ProRataCalculationQuery(ProRataCalculationRequestDto request)
        : IRequest<Result<object>>
    {
        public ProRataCalculationRequestDto Request { get; set; } = request;
    }
}
