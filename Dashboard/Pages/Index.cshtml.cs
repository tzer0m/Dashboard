using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dashboard.Pages;

/// <summary>
/// Page model for the main dashboard, grouped by device.
/// </summary>
/// <remarks>
/// Initialises a new instance of <see cref="IndexModel"/>.
/// </remarks>
/// <param name="statusStore">The singleton status store.</param>
/// <param name="configuration">The app configuration.</param>
public class IndexModel(StatusStore statusStore, IConfiguration configuration) : PageModel
{
    private readonly StatusStore StatusStore = statusStore;
    private readonly IConfiguration Configuration = configuration;

    /// <summary>
    /// Services grouped by device name, populated on GET.
    /// </summary>
    public Dictionary<string, List<ServiceEntry>> ServicesByDevice { get; set; } = [];

    /// <summary>
    /// Latest cached status for all services, keyed by service name.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceStatus> Statuses { get; set; } = new Dictionary<string, ServiceStatus>();

    /// <summary>
    /// Loads services from config and reads cached statuses from the store.
    /// </summary>
    public void OnGet()
    {
        List<ServiceEntry> services = Configuration.GetSection("Services").Get<List<ServiceEntry>>() ?? [];
        ServicesByDevice = services.GroupBy(s => s.Device).ToDictionary(g => g.Key, g => g.ToList());
        Statuses = StatusStore.GetAll();
    }
}