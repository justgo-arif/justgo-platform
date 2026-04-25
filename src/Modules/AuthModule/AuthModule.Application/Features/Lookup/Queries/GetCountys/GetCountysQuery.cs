using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetCountys
{
    public class GetCountysQuery : IRequest<List<SelectListItemDTO<string>>>
    {
        public GetCountysQuery()
        {
        }

    }
}
