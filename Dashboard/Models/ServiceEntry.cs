namespace Dashboard.Models;

/// <summary>
/// Represents a single service entry with its static configuration.
/// </summary>
public class ServiceEntry
{
    /// <summary>
    /// Gets or sets the display name of the service.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public-facing URL of the service.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of service (e.g. Website, API, Media Server).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the service is globally or locally accessible.
    /// </summary>
    public string Access { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the device hosting this service.
    /// </summary>
    public string Device { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local IP address of the host device.
    /// </summary>
    public string LocalIp { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local port the service runs on.
    /// </summary>
    public int? LocalPort { get; set; }
}