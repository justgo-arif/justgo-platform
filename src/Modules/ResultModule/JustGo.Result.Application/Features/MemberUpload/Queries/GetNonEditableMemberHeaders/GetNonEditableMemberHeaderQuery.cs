using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetNonEditableMemberHeaders
{
    public record GetNonEditableMemberHeaderQuery : IRequest<List<string>>
    {
        public SportType SportType { get; set; }
    }
}
