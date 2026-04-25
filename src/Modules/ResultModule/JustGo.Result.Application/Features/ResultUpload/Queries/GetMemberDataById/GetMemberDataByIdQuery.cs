using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById;

public class GetMemberDataByIdQuery : IRequest<Result<object>>
{
    public GetMemberDataByIdQuery(int memberDataId, SportType sportType)
    {
        MemberDataId = memberDataId;
        SportType = sportType;
    }

    public int MemberDataId { get; set; }
    public SportType SportType { get; }
}