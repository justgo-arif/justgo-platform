using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetProductCategory
{
    public class GetProductCategoryQuery : IRequest<List<LookupIntDto>>
    {
    }
}
