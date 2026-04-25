namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands;


public sealed record FileProcessingContext
{

    public required (string Name, int Id) Discipline { get; init; }

    public required int EventId { get; init; }
    public required int UploadedFileId { get; init; }
}