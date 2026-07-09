namespace Dashboard.Models;

/// <summary>
/// Represents the result of a single health check against a monitored service, used to compute rolling uptime percentages and outage history.
/// </summary>
public sealed class UptimeRecord
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the service that was checked, matching <see cref="ServiceEntry.Name"/>.
    /// </summary>
    public required string ServiceName { get; set; }

    /// <summary>
    /// The UTC timestamp at which the check was performed.
    /// </summary>
    public DateTime CheckedAtUtc { get; set; }

    /// <summary>
    /// Whether the service was considered up at the time of the check.
    /// </summary>
    public bool IsUp { get; set; }

    /// <summary>
    /// The HTTP status code returned by the check, if applicable.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// The response time of the check, in milliseconds.
    /// </summary>
    public int? ResponseTimeMs { get; set; }
}