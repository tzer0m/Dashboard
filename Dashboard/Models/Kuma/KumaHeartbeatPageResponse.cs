namespace Dashboard.Models.Kuma;

/// <summary>
/// The response from Kuma's public status page heartbeat endpoint (<c>/api/status-page/heartbeat/{slug}</c>).
/// </summary>
public sealed class KumaHeartbeatPageResponse
{
    /// <summary>
    /// Recent heartbeats for each monitor, keyed by the monitor's ID as a string, oldest first.
    /// </summary>
    public Dictionary<string, List<KumaHeartbeat>> HeartbeatList { get; init; } = [];
}