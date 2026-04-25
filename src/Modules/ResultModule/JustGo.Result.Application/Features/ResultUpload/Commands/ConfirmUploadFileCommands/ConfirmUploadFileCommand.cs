using System.Text.Json.Serialization;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands;

public class ConfirmUploadFileCommand : IRequest<Result<int>>
{
    public ConfirmUploadFileCommand(int uploadFileId, SportType sportType)
    {
        UploadFileId = uploadFileId;
        SportType = sportType;
    }

    public int UploadFileId { get; }
    
    [JsonIgnore]
    public SportType SportType { get; }
}