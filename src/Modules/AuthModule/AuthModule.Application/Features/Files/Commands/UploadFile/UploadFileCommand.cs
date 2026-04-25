using AuthModule.Application.DTOs.Stores;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Commands.UploadFile;

public class UploadFileCommand : IRequest<UploadFileResultDto>
{
    public string EntityType { get; set; }
    public string ClientUploaderRef { get; set; }
    public string UseTemp { get; set; }
    public string SuccessReturnAction { get; set; }
    public string ErrorCallBackMethod { get; set; }
    public string CustomStorePath { get; set; }
    public string FileName { get; set; }
    public byte[] FileBytes { get; set; }
}
