using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Commands.UploadBase64File;

public class UploadBase64FileCommand : IRequest<string>
{
    public string Base64String { get; set; }
    public string T { get; set; }
    public string P { get; set; }
    public string P1 { get; set; }
}
