namespace Dashboard.Models.Kuma;

/// <summary>
/// A single heartbeat entry as returned by Kuma's public status page heartbeat endpoint.
/// </summary>
public sealed class KumaHeartbeat
{
    /// <summary>
    /// The heartbeat status: 0 = down, 1 = up, 2 = pending, 3 = maintenance.
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// The check result message, empty when up and nothing noteworthy occurred.
    /// </summary>
    public string? Msg { get; init; }

    /// <summary>
    /// The response time in milliseconds, if applicable to the monitor type.
    /// </summary>
    public int? Ping { get; init; }
}