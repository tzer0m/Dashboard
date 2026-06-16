namespace Dashboard.Models;

/// <summary>
/// Represents the live health check result for a single service.
/// </summary>
public class ServiceStatus
{
    /// <summary>
    /// Gets or sets whether the service responded successfully.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code returned by the service, if any.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the last health check.
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// Gets or sets the error message if the service was unreachable, otherwise null.
    /// </summary>
    public string? Error { get; set; }
}