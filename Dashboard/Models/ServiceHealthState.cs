namespace Dashboard.Models;

/// <summary>
/// Tracks consecutive failure state for a monitored service, used to decide when to send Ting notifications.
/// </summary>
public class ServiceHealthState
{
    /// <summary>
    /// Number of consecutive failed checks for this service.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Whether a "service down" notification has already been sent for the current outage.
    /// </summary>
    public bool HasNotifiedDown { get; set; }
}