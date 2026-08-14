namespace Dashboard.Models.Kuma;

/// <summary>
/// A named group of monitors as organised on a Kuma status page.
/// </summary>
public sealed class KumaGroup
{
    /// <summary>
    /// The monitors belonging to this group.
    /// </summary>
    public List<KumaMonitorInfo> MonitorList { get; init; } = [];
}