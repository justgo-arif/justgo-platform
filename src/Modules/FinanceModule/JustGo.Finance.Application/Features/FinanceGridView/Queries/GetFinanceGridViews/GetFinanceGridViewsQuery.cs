using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.FinanceGridViewDtos;

namespace JustGo.Finance.Application.Features.FinanceGridView.Queries.GetFinanceGridViews
{
    public record GetFinanceGridViewsQuery( 
        string MerchantId,
        int EntityType
    ) : IRequest<List<FinanceGridViewDto>?>;
}
