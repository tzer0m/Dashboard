using System.Collections.Concurrent;
using Dashboard.Models.Uptime;

namespace Dashboard.Services;

/// <summary>
/// Singleton in-memory store for cached 30-day uptime summaries. Written to by <see cref="HealthCheckService"/> and read by page models.
/// </summary>
public class UptimeSummaryStore
{
    private readonly ConcurrentDictionary<string, UptimeSummary> Summaries = new();

    /// <summary>
    /// Updates or inserts the uptime summary for a given service.
    /// </summary>
    /// <param name="serviceName">The unique name of the service.</param>
    /// <param name="summary">The latest computed uptime summary.</param>
    public void Set(string serviceName, UptimeSummary summary)
    {
        Summaries[serviceName] = summary;
    }

    /// <summary>
    /// Retrieves the latest uptime summary for a given service, or null if not yet computed.
    /// </summary>
    /// <param name="serviceName">The unique name of the service.</param>
    /// <returns>The cached <see cref="UptimeSummary"/>, or null if unavailable.</returns>
    public UptimeSummary? Get(string serviceName)
    {
        Summaries.TryGetValue(serviceName, out UptimeSummary? summary);
        return summary;
    }

    /// <summary>
    /// Returns a snapshot of all current uptime summaries.
    /// </summary>
    /// <returns>A read-only dictionary of service names to their uptime summaries.</returns>
    public IReadOnlyDictionary<string, UptimeSummary> GetAll()
    {
        return Summaries;
    }
}