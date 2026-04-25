using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventCategorys
{
    public class GetEventCategorysQuery : IRequest<List<SelectListItemDTO<string>>>
    {
        public GetEventCategorysQuery(string? resultEventTypeId)
        {
            ResultEventTypeId = resultEventTypeId;
        }

        public string? ResultEventTypeId { get; }
    }
}
