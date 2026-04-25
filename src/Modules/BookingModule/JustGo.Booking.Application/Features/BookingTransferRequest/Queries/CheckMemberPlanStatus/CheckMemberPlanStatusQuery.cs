using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingTransferRequestDTOs;

namespace JustGo.Booking.Application.Features.BookingTransferRequest.Queries.CheckMemberPlanStatus
{
    public record CheckMemberPlanStatusQuery : IRequest<MemberPlanStatusResultDto>
    {
        public required string sessionGuid { get; init; }
        public required List<int> MemberDocIds { get; init; }
    }
}
