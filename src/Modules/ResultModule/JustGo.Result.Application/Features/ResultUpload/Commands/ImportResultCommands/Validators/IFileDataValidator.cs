namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.Validators;

public interface IFileDataValidator
{
    ICollection<string> ValidateRow(Dictionary<string, string> rowData);
}