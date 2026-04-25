namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public record FilePreviewDto
{
    public List<ResultMemberDataDto> PreviewData { get; set; } = [];
    public IEnumerable<string> EditableHeaders { get; set; } = [];
}