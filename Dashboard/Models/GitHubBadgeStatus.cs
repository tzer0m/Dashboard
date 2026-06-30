namespace Dashboard.Models;

/// <summary>
/// Represents the shields.io-compatible badge status for a service's latest GitHub Actions workflow run.
/// </summary>
public sealed class GitHubBadgeStatus
{
    /// <summary>
    /// The badge message text, e.g. "passing", "failing", "running", or "unknown".
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The badge colour as a hex string without the leading "#", e.g. "198754".
    /// </summary>
    public required string Color { get; init; }
}