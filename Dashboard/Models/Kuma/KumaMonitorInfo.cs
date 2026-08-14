namespace Dashboard.Models.Kuma;

/// <summary>
/// A single monitor's static info as listed on a Kuma status page.
/// </summary>
public sealed class KumaMonitorInfo
{
    /// <summary>
    /// The monitor's numeric ID in Kuma, used to look up its heartbeats.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The monitor's display name in Kuma.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}