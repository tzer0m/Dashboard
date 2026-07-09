namespace Dashboard.Models.Uptime;

/// <summary>
/// The computed 30-day uptime summary for a single service.
/// </summary>
/// <param name="UptimePercent">
/// The percentage of known time the service was up over the window, excluding
/// any periods where no data was recorded. Null if no known time exists at all.
/// </param>
/// <param name="Days">The 30 daily bars, oldest first.</param>
public sealed record UptimeSummary(double? UptimePercent, IReadOnlyList<DailyUptimeBar> Days);