namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;


public record ClassCategoryInfo
{

    public string ClassCategoryName { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string Official { get; set; } = string.Empty;
    public string ClassDate { get; set; } = string.Empty;
    
    public string ShowJumpingBeforeXc { get; set; } = string.Empty;

    public string FirstHiOrder { get; set; } = string.Empty; 

    public string SecondHiOrder { get; set; } = string.Empty; 

    public string IsChampionship { get; set; } = string.Empty;
    
    public virtual bool Equals(ClassCategoryInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(ClassCategoryName, other.ClassCategoryName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(ClassName, other.ClassName, StringComparison.OrdinalIgnoreCase);
    }


    public override int GetHashCode() => HashCode.Combine(
        ClassCategoryName?.ToUpperInvariant(),
        ClassName?.ToUpperInvariant());
}