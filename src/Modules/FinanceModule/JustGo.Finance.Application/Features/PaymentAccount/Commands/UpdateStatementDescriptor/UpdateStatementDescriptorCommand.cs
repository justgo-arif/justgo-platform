using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateStatementDescriptor;

public class UpdateStatementDescriptorCommand : IRequest<bool>
{
    public Guid MerchantId { get; set; }
    public string? StatementDescriptor { get; set; }
}
