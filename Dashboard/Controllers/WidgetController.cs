using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

/// <summary>
/// Serves the public status summary for the Tidgets Dashboard widget.
/// </summary>
/// <param name="hostStore">The store holding the hosts and their services.</param>
/// <param name="statusStore">The singleton status store.</param>
[ApiController]
[Route("api/widget")]
public sealed class WidgetController(HostStore hostStore, StatusStore statusStore) : ControllerBase
{
    /// <summary>
    /// Holds the hosts and their services.
    /// </summary>
    private readonly HostStore HostStore = hostStore;

    /// <summary>
    /// Stores the current status of every monitor.
    /// </summary>
    private readonly StatusStore StatusStore = statusStore;

    /// <summary>
    /// Returns the online and offline counts overall and per host, counting extra monitors too.
    /// </summary>
    [HttpGet]
    public ActionResult<WidgetSummary> Get()
    {
        List<HostEntry> hosts = [.. HostStore.GetAll().OrderBy(h => h.Host)];
        IReadOnlyDictionary<string, ServiceStatus> statuses = StatusStore.GetAll();
        WidgetSummary summary = new();
        foreach (HostEntry host in hosts)
        {
            List<string> monitors = [.. host.Services.SelectMany(s => s.MonitorNames)];
            int online = monitors.Count(m => statuses.GetValueOrDefault(m)?.IsOnline == true);
            int offline = monitors.Count(m => statuses.GetValueOrDefault(m)?.IsOnline == false);
            summary.Hosts.Add(new WidgetHost { Short = ShortName(host.Host, hosts), Online = online, Offline = offline });
            summary.Online += online;
            summary.Offline += offline;
        }
        return Ok(summary);
    }

    /// <summary>
    /// Returns the shortest start of a host's name that no other host's name starts with, such as Ty for Tyrion alongside TzerDM2.
    /// </summary>
    /// <param name="name">The host's name.</param>
    /// <param name="hosts">Every host, to check the prefix against.</param>
    private static string ShortName(string name, List<HostEntry> hosts)
    {
        for (int length = 1; length < name.Length; length++)
        {
            string prefix = name[..length];
            if (!hosts.Any(h => h.Host != name && h.Host.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return prefix;
            }
        }
        return name;
    }
}