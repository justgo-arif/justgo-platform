using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.RevalidateMemberCommands
{
    public class RevalidateMemberCommand(List<RevalidateMemberDataDto> items) : IRequest<Result<string>>
    {
        public int FileId { get; set; }
        public List<RevalidateMemberDataDto> Items { get; set; } = items;
    }
}
