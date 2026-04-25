using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMyClubsBySyncGuid
{
    public class GetMyClubsBySyncGuidQuery : IRequest<List<ClubInfoDto>>
    {
        public GetMyClubsBySyncGuidQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}