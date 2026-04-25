using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Commands.CancelledInstallment
{
    public record UpdatePlanStatusCommand(Guid MerchantId, Guid PlanId, bool IsActive,RecurringType RecurringType) : IRequest<bool>;
}
 