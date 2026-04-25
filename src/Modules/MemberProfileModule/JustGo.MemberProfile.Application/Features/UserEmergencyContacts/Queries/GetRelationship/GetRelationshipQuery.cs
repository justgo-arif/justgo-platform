using JustGo.MemberProfile.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetRelationship
{
    public class GetRelationshipQuery : IRequest<List<UserRelationshipDto>>
    {
        public GetRelationshipQuery()
        {
        }

    }
}