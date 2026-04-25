using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.DeleteMember;

public class DeleteMemberCommand(int fileId, ICollection<int> memberDataIds) : IRequest<Result<bool>>
{
    public int FileId { get; } = fileId;
    public ICollection<int> MemberDataIds { get; } = memberDataIds;
}