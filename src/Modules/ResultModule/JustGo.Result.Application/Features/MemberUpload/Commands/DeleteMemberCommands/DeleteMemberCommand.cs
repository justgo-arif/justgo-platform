using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DeleteMemberCommands
{
    public class DeleteMemberCommand(List<int> uploadedMemberIds) : IRequest<Result<string>>
    {
        public List<int> UploadedMemberIds { get; set; } = uploadedMemberIds;
    }
}
