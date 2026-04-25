using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;
namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyJoinRequest;

public class GetFamilyJoinRequestQuery : IRequest<List<FamilyJoinRequestDto>>
{ 
    public Guid Id { get; set; }
    public GetFamilyJoinRequestQuery(Guid id)
    {
        Id = id;
    }
}