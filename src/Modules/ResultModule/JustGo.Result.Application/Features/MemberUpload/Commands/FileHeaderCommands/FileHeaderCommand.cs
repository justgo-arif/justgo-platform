using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.FileHeaderCommands
{
    public class FileHeaderCommand : IRequest<Result<FileHeaderResponseDto>>
    {
        public FileHeaderRequestDto FileDto { get; set; }
        public int OwnerId { get; set; }
        public FileHeaderCommand(FileHeaderRequestDto fileDto, int ownerId)
        {
            FileDto = fileDto;
            OwnerId = ownerId;
        }
    }
}
