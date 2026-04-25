using System.Runtime.InteropServices.JavaScript;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;

public record RoundInfo
{
    public required string RoundName { get; init; }
    public required DateTime RoundStartDate { get; init; }
    public required DateTime RoundEndDate { get; set; }
}