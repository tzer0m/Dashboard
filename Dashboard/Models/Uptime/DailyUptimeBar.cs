namespace Dashboard.Models.Uptime;

/// <summary>
/// The uptime classification for a single calendar day, used to render the 30-day bar strip.
/// </summary>
/// <param name="Date">The calendar date this bar represents.</param>
/// <param name="Status">The worst-case status recorded during this day.</param>
public sealed record DailyUptimeBar(DateOnly Date, UptimeDayStatus Status);