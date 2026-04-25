using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Commands.CancelPlan
{
    public record CancelPlanCommand(Guid MerchantId, Guid PlanId, RecurringType ScheduleRecurringType, string? CancellationReason) : IRequest<bool>;
}
