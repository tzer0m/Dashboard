using System.Collections.Concurrent;
using Dashboard.Models;

namespace Dashboard.Services;

/// <summary>
/// Singleton in-memory store for cached service health check results.
/// Written to by <see cref="HealthCheckService"/> and read by page models.
/// </summary>
public class StatusStore
{
    private readonly ConcurrentDictionary<string, ServiceStatus> Statuses = new();

    /// <summary>
    /// Updates or inserts the status for a given service.
    /// </summary>
    /// <param name="serviceName">The unique name of the service.</param>
    /// <param name="status">The latest health check result.</param>
    public void Set(string serviceName, ServiceStatus status)
    {
        Statuses[serviceName] = status;
    }

    /// <summary>
    /// Retrieves the latest status for a given service, or null if not yet checked.
    /// </summary>
    /// <param name="serviceName">The unique name of the service.</param>
    /// <returns>The cached <see cref="ServiceStatus"/>, or null if unavailable.</returns>
    public ServiceStatus? Get(string serviceName)
    {
        Statuses.TryGetValue(serviceName, out ServiceStatus? status);
        return status;
    }

    /// <summary>
    /// Returns a snapshot of all current service statuses.
    /// </summary>
    /// <returns>A read-only dictionary of service names to their statuses.</returns>
    public IReadOnlyDictionary<string, ServiceStatus> GetAll()
    {
        return Statuses;
    }
}