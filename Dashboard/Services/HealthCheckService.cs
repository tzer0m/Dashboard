using Dashboard.Models;
using System.Diagnostics;
using System.Net;

namespace Dashboard.Services;

/// <summary>
/// Background service that pings all configured services every 60 seconds
/// and stores the results in <see cref="StatusStore"/>.
/// </summary>
public class HealthCheckService : BackgroundService
{
    /// <summary>
    /// Http client used to send requests to the services.
    /// </summary>
    private readonly HttpClient HttpClient;

    /// <summary>
    /// Memory cache used to store the results of the health checks.
    /// </summary>
    private readonly StatusStore StatusStore;

    /// <summary>
    /// List of services to monitor, loaded from configuration.
    /// </summary>
    private readonly List<ServiceEntry> Services;

    /// <summary>
    /// The configured service entries being monitored.
    /// </summary>
    public IReadOnlyList<ServiceEntry> ServiceEntries => Services;

    /// <summary>
    /// Logger used to log information and errors.
    /// </summary>
    private readonly ILogger<HealthCheckService> Logger;

    /// <summary>
    /// Interval between health checks, in milliseconds. This is set to 60 seconds unless overridden.
    /// </summary>
    private readonly int IntervalSeconds;

    /// <summary>
    /// Initialises a new instance of <see cref="HealthCheckService"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to ping services.</param>
    /// <param name="statusStore">The singleton store for caching results.</param>
    /// <param name="configuration">The app configuration containing service entries.</param>
    /// <param name="logger">The logger instance.</param>
    public HealthCheckService(HttpClient httpClient, StatusStore statusStore, IConfiguration configuration, ILogger<HealthCheckService> logger)
    {
        HttpClient = httpClient;
        HttpClient.Timeout = TimeSpan.FromSeconds(5);
        StatusStore = statusStore;
        Logger = logger;
        Services = configuration.GetSection("Services").Get<List<ServiceEntry>>() ?? [];
        IntervalSeconds = configuration.GetValue("HealthCheck:IntervalSeconds", 60);
    }

    /// <summary>
    /// Executes the background ping loop, running once immediately then every 60 seconds.
    /// </summary>
    /// <param name="stoppingToken">Token that signals when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PingAllServicesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
        }
    }

    /// <summary>
    /// Pings all configured services concurrently and updates the status store.
    /// </summary>
    /// <param name="cancellationToken">Token that signals when the host is shutting down.</param>
    private async Task PingAllServicesAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(Services.Select(service => PingServiceAsync(service, cancellationToken)));
    }

    /// <summary>
    /// Pings a single service and stores the result.
    /// </summary>
    /// <param name="service">The service to ping.</param>
    /// <param name="cancellationToken">Token that signals when the host is shutting down.</param>
    private async Task PingServiceAsync(ServiceEntry service, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            // Send a GET request to the service URL
            HttpResponseMessage response = await HttpClient.GetAsync(service.Url, cancellationToken);
            stopwatch.Stop();

            // Store the result in the status store
            bool isOnline = response.IsSuccessStatusCode || (service.AuthRequired && response.StatusCode == HttpStatusCode.Unauthorized);
            StatusStore.Set(service.Name, new ServiceStatus
            {
                IsOnline = isOnline,
                StatusCode = (int)response.StatusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow,
                Error = isOnline ? null : $"HTTP {(int)response.StatusCode}"
            });
        }
        catch (Exception ex)
        {
            // Log the error and store the failure in the status store
            stopwatch.Stop();
            Logger.LogWarning("Health check failed for {ServiceName}: {Message}", service.Name, ex.Message);

            // Store the failure in the status store
            StatusStore.Set(service.Name, new ServiceStatus
            {
                IsOnline = false,
                StatusCode = null,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }
}