using JustGo.MemberProfile.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.FamilyActionToken
{
    public class FamilyActionTokenQuery : IRequest<ActionTokenHandlerResponse>
    {
        public required string Token { get; set; }
    }
}
