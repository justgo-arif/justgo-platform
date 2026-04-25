using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.CreateUpdate
{
    public record CreateUpdateFinanceGridViewCommand(
        int? ViewId, 
        string Name,
        dynamic Payload, 
        string MerchantId,
        int EntityType,  
        bool IsSystemDefault,
        bool IsPinned, 
        bool IsShared
    ) : IRequest<bool>;
}
