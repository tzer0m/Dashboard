namespace Dashboard.Models;

/// <summary>
/// View model for the service card partial.
/// </summary>
public class ServiceCard
{
    /// <summary>
    /// Gets or sets the service entry with its static configuration.
    /// </summary>
    public ServiceEntry Service { get; set; } = new();

    /// <summary>
    /// Gets or sets the latest status of every monitor, keyed by monitor name.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceStatus> Statuses { get; set; } = new Dictionary<string, ServiceStatus>();

    /// <summary>
    /// Gets or sets an optional favicon URL override.
    /// </summary>
    public string? FaviconUrl { get; set; }
}