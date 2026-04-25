using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Membership.Application.DTOs;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMerchandiseItems
{
    public class GetMerchandiseItemsQuery : IRequest<List<MerchandiseItemsDto>>
    {
        public GetMerchandiseItemsQuery(IEnumerable<string> ids)
        {
            Ids = ids.ToList();
        }

        public List<string> Ids { get; set; }
    }
}