using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventYears
{
    public class GetEventYearsQuery : IRequest<List<SelectListItemDTO<string>>>
    {
        public GetEventYearsQuery()
        {
        }
    }
}