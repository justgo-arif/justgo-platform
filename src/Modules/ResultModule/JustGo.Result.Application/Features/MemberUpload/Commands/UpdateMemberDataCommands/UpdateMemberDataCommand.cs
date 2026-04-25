using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.UpdateMemberDataCommands
{
    public class UpdateMemberDataCommand : IRequest<Result<string>>
    {
        public UpdateMemberDataDto UpdateMemberDataDto { get; set; }

        public UpdateMemberDataCommand(UpdateMemberDataDto updateMemberDataDto)
        {
            UpdateMemberDataDto = updateMemberDataDto;
        }
        public UpdateMemberDataCommand() { }
    }
}
