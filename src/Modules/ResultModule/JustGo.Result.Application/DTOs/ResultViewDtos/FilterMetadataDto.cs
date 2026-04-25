namespace JustGo.Result.Application.DTOs.ResultViewDtos;

public class FilterMetadataDto
{
    public required Dictionary<string, List<string>> FilterOptions { get; init; }
}