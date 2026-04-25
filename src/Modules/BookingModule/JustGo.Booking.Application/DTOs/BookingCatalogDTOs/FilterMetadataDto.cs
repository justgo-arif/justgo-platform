using System.Collections.Frozen;
using JustGoAPI.Shared.Miscellaneous;

namespace JustGo.Booking.Application.DTOs.BookingCatalogDTOs;

public sealed record FilterMetadataDto
{
    public FrozenSet<KeyValue<string, string>>? Weekdays { get; set; } 
    public required IReadOnlyList<KeyValue<string, int>> AgeGroups { get; init; } = [];
    public required IReadOnlyList<KeyValue<string, int>> Disciplines { get; init; } = [];
    public required IReadOnlyList<KeyValue<string, int>> ClassGroups { get; init; } = [];
    public FrozenSet<KeyValue<string, string>>? TimeOfDays { get; set; } 
    public required IReadOnlyList<ColorGroup> ColorGroups { get; init; } = [];
    public string[]? GenderOptions { get; set; } 
    public FrozenSet<KeyValue<string, int>>? PriceOptions { get; set; } 
    public FrozenSet<KeyValue<string, int>>? Duration { get; set; } 
}

public sealed record ColorGroup
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string HexCode { get; init; }
}