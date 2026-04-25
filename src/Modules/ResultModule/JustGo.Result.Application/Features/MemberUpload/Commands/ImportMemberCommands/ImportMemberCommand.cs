using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.ImportMemberCommands
{

    public class ImportMemberCommand : IRequest<Result<string>>
    {
        public ConfirmMemberFileDto FileDto { get; set; }
        public string OperationId { get; set; }
        public ImportMemberCommand(ConfirmMemberFileDto fileDto, string operationId)
        {
            FileDto = fileDto;
            OperationId = operationId;
        }
    }

}
