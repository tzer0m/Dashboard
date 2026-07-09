namespace Dashboard.Models.Uptime;

/// <summary>
/// The worst-case classification of a single day's uptime for the 30-day bar strip.
/// </summary>
public enum UptimeDayStatus
{
    /// <summary>No outages or gaps recorded for this day.</summary>
    Up,

    /// <summary>No data was recorded for part of this day (health check gap).</summary>
    Unknown,

    /// <summary>At least one outage occurred during this day.</summary>
    Down
}