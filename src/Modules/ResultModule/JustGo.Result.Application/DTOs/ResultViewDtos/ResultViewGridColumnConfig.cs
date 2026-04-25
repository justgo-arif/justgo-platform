namespace JustGo.Result.Application.DTOs.ResultViewDtos;

public class ResultViewGridColumnConfig
{
    public required string ColName { get; set; }
    public required string ColCaption { get; set; }
    public bool? IsSortable { get; set; }
    public int Sequence { get; set; }
    public int? ColType { get; set; }
    public bool? IsClickable { get; set; }
    public List<string>? Fields { get; set; }
    public int ConWidth { get; set; }
}