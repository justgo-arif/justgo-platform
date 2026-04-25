using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.FinanceGridViewDtos;

namespace JustGo.Finance.Application.Features.FinanceGridView.Queries.GetFinanceGridViewById
{
    public record GetFinanceGridViewByIdQuery(
        int ViewId
    ) : IRequest<FinanceGridViewDto?>;
}
