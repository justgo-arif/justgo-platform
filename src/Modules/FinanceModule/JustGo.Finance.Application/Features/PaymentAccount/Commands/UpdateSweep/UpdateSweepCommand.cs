using Adyen.Model.BalancePlatform;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateSweep;

public class UpdateSweepCommand : IRequest<bool>
{
    public required string BalanceAccountId { get; set; }
    public required string SweepId { get; set; }
    public required SweepSchedule.TypeEnum SweepType { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
}
