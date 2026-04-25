namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;

public record ClassInfo
{
    public string? ClassName { get; init; } = string.Empty;

    // public string Official { get; set; } = string.Empty;
    public string? CompStartDate { get; set; }
    public string? CompEndDate { get; set; }
    public string AdditionalData { get; set; } = string.Empty;

    public virtual bool Equals(ClassInfo? other)
    {
        return other is not null && string.Equals(ClassName, other.ClassName, StringComparison.OrdinalIgnoreCase);
    }


    public override int GetHashCode() => HashCode.Combine(
        ClassName?.ToUpperInvariant());
}