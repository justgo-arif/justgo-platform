using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;

public class ImportResultFileCommand
    : IRequest<Result<string>>
{
    public ImportResultFileCommand(ConfirmMemberFileDto fileDto,
        SportType sportType)
    {
        FileDto = fileDto;
        SportType = sportType;
    }

    public ConfirmMemberFileDto FileDto { get; set; }   
    public SportType SportType { get; init; }
}