public class CompetitionCreateMetadataDto
{
    public List<TimeZoneDto> TimeZones { get; set; }
    public List<CategoryDto> Categories { get; set; }
    public List<ResultEventTypeDto> ResultEventTypes { get; set; }
}

public class TimeZoneDto
{
    public int ZoneId { get; set; }
    public string Abbreviation { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public decimal Offset { get; set; }
    public bool Dst { get; set; }
    public string TZDBZoneIdentifier { get; set; } = string.Empty;
}

public class CategoryDto
{
    public int EventCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? RecordGuid { get; set; } = string.Empty;
    public int ResultEventTypeId { get; set; }
}

public class ResultEventTypeDto
{
    public int ResultEventTypeId { get; set; }
    public string RecordGuid { get; set; }=string.Empty;
    public string TypeName { get; set; }=string.Empty;
    public string Caption { get; set; }=string.Empty;
}
