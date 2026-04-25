namespace JustGo.Result.Application.DTOs.ImportExportFileDtos;

public record ResolvedValidationScopedDto
{
    public IList<ValidationScopeFieldMappingDto> ValidationScopeFieldMappings { get; init; } = [];
    public IList<int> ValidationScopeIds { get; init; } = [];
    public IList<(int ValidationScopeId, string ValidationItemDisplayName)> ValidatedMemberIdHeaders { get; init; } = [];
    public string ValidatedAssetIdHeader { get; init; } = string.Empty;
    public bool ShouldResolveValidationScope { get; init; }
    public string? HeaderName { get; init; }
}