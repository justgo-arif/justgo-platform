using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands;

public interface IUploadResultFileProcessor
{
    Task<Result<FileHeaderResponseDto>> ProcessAsync(UploadResultFileCommand request,
        CancellationToken cancellationToken);
}