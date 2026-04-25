using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Commands.UpdatePaymentSchedule
{
    public record UpdatePaymentScheduleCommand(Guid MerchantId, int PlanId, PaymentDateUpdateRequest UpdateRequest) : IRequest<bool>;
}
