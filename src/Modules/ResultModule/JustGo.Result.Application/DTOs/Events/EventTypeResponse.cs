using System.Text.Json.Serialization;

namespace JustGo.Result.Application.DTOs.Events;


public class EventTypeResponse
{
    public string Id { get; set; } = default!;
    public string Text { get; set; } = default!;
    public string Caption { get; set; } = default!;
    public string ConfigJson { get; set; } = default!;
    public Config Config { get; set; } = default!;
}

public class Config
{
    public string ThemeColor { get; set; } = default!;
    public string LogoUrl { get; set; } = default!;
    public List<SubMenuItem> SubMenu { get; set; } = new();
}

public class SubMenuItem
{
    public string Id { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string SearchPlaceholder { get; set; } = default!;
    public string PlaceHolderImage { get; set; } = default!;
}