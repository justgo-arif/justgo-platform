using System.Collections.Frozen;

namespace JustGoAPI.Shared.Miscellaneous;

public static class ReferenceData
{
    public static readonly FrozenSet<KeyValue<string, int>> DefaultDurations =
    [
        new("30 minutes", 30),
        new("1 hour", 60),
        new("1.5 hours", 90),
        new("2 hours", 120),
        new("2.5 hours", 150),
        new("3 hours", 180),
        new("3.5 hours", 210),
        new("4+ hours", 240)
    ];


    public static readonly FrozenSet<KeyValue<string, string>> DefaultWeekdays =
    [
        new("MON", "mon"),
        new("TUE", "tue"),
        new("WED", "wed"),
        new("THU", "thu"),
        new("FRI", "fri"),
        new("SAT", "sat"),
        new("SUN", "sun"),
    ];

    public static readonly FrozenSet<KeyValue<string, string>> DefaultTimeOfDays =
    [
        new("Morning", "00:00-11:59"),
        new("Afternoon", "12:00-17:59"),
        new("Evening", "18:00-23:59")
    ];

    public static readonly FrozenSet<KeyValue<string, int>> DefaultPriceOptions =
    [
        new("Pay now for all sessions", 1),
        new("Book all sessions and pay monthly", 2),
        new("Pay as you go by booking a single session", 3),
        new("Book a trial", 4)
    ];
}