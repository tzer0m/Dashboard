namespace Dashboard.Models;

/// <summary>
/// Represents a host device and the services running on it.
/// </summary>
public class HostEntry
{
    /// <summary>
    /// Gets or sets the name of the host device.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local IP address of the host device.
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the services running on this host.
    /// </summary>
    public List<ServiceEntry> Services { get; set; } = [];
}