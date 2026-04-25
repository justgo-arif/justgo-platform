namespace JustGo.Finance.Domain.Entities;

public class CronSchedule
{
    public string? PayoutSchedule { get; set; } // "Daily", "Weekly", "Monthly", "Custom"
    public int? DayOfWeek { get; set; }        // 0=Sunday, 1=Monday, ..., 6=Saturday (Cron standard)
    public int? DayOfMonth { get; set; }       // 1 to 31
}
