using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands;

public class UploadResultFileCommand : IRequest<Result<FileHeaderResponseDto>>
{
    public UploadResultFileCommand(UploadResultFileDto fileDto, SportType sportType)
    {
        FileDto = fileDto;
        SportType = sportType;
    }

    public SportType SportType { get; init; }
    public UploadResultFileDto FileDto { get; set; }
}