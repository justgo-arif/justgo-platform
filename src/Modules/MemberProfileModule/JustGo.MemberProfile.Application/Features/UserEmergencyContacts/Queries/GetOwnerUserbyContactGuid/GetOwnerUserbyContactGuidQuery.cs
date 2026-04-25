using JustGo.MemberProfile.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetOwnerUserbyContactGuid 
{
    public class GetOwnerUserbyContactGuidQuery : IRequest<UserBasicInfoDTO>
    {
        public GetOwnerUserbyContactGuidQuery(Guid id)
        {
            Id = id;
        }
        public Guid Id { get; set; }
    }
}
