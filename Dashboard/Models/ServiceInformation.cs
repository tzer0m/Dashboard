namespace Dashboard.Models;

/// <summary>
/// Combined health, deploy status, and static configuration for a single service, as exposed by the dashboard status API.
/// </summary>
public sealed class ServiceInformation
{
    /// <summary>
    /// The service's display name, matching <see cref="ServiceEntry.Name"/>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The public-facing URL of the service.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The type of service, e.g. "Website", "API", "Media Server".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Whether the service is globally or locally accessible.
    /// </summary>
    public required string Access { get; init; }

    /// <summary>
    /// The name of the device hosting this service, e.g. "Tyrion", "Robert".
    /// </summary>
    public required string Device { get; init; }

    /// <summary>
    /// The local IP address of the host device.
    /// </summary>
    public required string LocalIp { get; init; }

    /// <summary>
    /// The local port the service runs on, if applicable.
    /// </summary>
    public required int? LocalPort { get; init; }

    /// <summary>
    /// An optional override URL for the service favicon. If set, used instead of auto-detection.
    /// </summary>
    public required string? FaviconUrl { get; init; }

    /// <summary>
    /// The GitHub repository name used for the deploy badge, or null if not configured.
    /// </summary>
    public required string? RepoName { get; init; }

    /// <summary>
    /// The latest health check result, or null if not yet checked.
    /// </summary>
    public required ServiceStatus? Health { get; init; }

    /// <summary>
    /// The latest GitHub Actions deploy status, or null if the service has no repo configured.
    /// </summary>
    public required GitHubBadgeStatus? DeployBadge { get; init; }
}