namespace Dashboard.Models;

/// <summary>
/// Combined health and deploy status for a single service, as exposed by the dashboard status API.
/// </summary>
public sealed class ServiceInformation
{
    /// <summary>
    /// The service's display name, matching <see cref="ServiceEntry.Name"/>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The latest health check result, or null if not yet checked.
    /// </summary>
    public required ServiceStatus? Health { get; init; }

    /// <summary>
    /// The latest GitHub Actions deploy status, or null if the service has no repo configured.
    /// </summary>
    public required GitHubBadgeStatus? DeployBadge { get; init; }
}