using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetGender
{
    public class GetGenderQuery : IRequest<List<SelectListItemDTO<string>>>
    {
        public GetGenderQuery()
        {
        }

    }
}


