using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.BulkRevalidateMemberCommands
{
    public class BulkRevalidateMemberCommand(int fileId, string webSocketId) : IRequest<Result<string>>
    {
        public int FileId { get; set; } = fileId;
        public required string WebSocketId { get; init; } = webSocketId;
    }
}
