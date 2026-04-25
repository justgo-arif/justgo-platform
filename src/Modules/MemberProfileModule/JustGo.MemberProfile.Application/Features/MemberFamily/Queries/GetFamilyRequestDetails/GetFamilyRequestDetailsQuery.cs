using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyRequestDetails;

public class GetFamilyRequestDetailsQuery : IRequest<List<FamilyRequestDetailsDto>>
{
    public Guid RecordGuid { get; set; }

    public GetFamilyRequestDetailsQuery(Guid recordGuid)
    {
        RecordGuid = recordGuid;
    }
}
