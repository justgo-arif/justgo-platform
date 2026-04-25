using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;

namespace AuthModule.Application.Features.Files.Commands.XUploadFile;

public class XUploadFileCommand : IRequest<string>
{
    public IFormFile File { get; set; }
    public string T { get; set; }
    public string P { get; set; }
}
