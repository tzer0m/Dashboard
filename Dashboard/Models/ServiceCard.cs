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
    /// Gets or sets the latest health check status, or null if not yet checked.
    /// </summary>
    public ServiceStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets an optional favicon URL override.
    /// </summary>
    public string? FaviconUrl { get; set; }
}