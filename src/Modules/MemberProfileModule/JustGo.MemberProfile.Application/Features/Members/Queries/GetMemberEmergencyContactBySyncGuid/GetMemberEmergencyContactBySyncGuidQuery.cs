
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberEmergencyContactBySyncGuid
{
    public class GetMemberEmergencyContactBySyncGuidQuery : IRequest<IEnumerable<UserEmergencyContact>>
    {
        public GetMemberEmergencyContactBySyncGuidQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}
