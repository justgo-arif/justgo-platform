using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetCountrys
{
    public class GetCountrysQuery : IRequest<List<SelectListItemDTO<string>>>
    {
        public GetCountrysQuery()
        {
        }

    }
}


