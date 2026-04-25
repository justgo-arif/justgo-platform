using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventDisciplinesList
{
    public class GetEventDisciplinesListQuery : IRequest<List<SelectListItemDTO<string>>>
    {
        public GetEventDisciplinesListQuery()
        {
        }
    }
}