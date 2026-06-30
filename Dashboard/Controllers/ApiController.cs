using Dashboard.Auth;
using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

/// <summary>
/// Exposes a JSON status endpoint combining service health and deploy badge data, for external consumers such as the Home Assistant integration.
/// </summary>
[ApiController]
[Route("[controller]")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public sealed class ApiController(HealthCheckService healthCheckService, GitHubBadgeService gitHubBadgeService, StatusStore statusStore) : ControllerBase
{
    /// <summary>
    /// Health check service used to retrieve the status of configured services.
    /// </summary>
    private readonly HealthCheckService HealthCheckService = healthCheckService;

    /// <summary>
    /// GitHub badge service used to retrieve deploy badge data for configured services.
    /// </summary>
    private readonly GitHubBadgeService GitHubBadgeService = gitHubBadgeService;

    /// <summary>
    /// Status store used to retrieve the last known status of services.
    /// </summary>
    private readonly StatusStore StatusStore = statusStore;

    /// <summary>
    /// Returns the current health and deploy status for all configured services.
    /// </summary>
    /// <param name="cancellationToken">Token that signals when the request is aborted.</param>
    [HttpGet("Status")]
    public async Task<ActionResult<List<ServiceInformation>>> GetStatusAsync(CancellationToken cancellationToken)
    {
        List<ServiceInformation> result = [];
        foreach (ServiceEntry service in HealthCheckService.ServiceEntries)
        {
            // Retrieve the last known status from the status store and the deploy badge data from GitHub
            ServiceStatus? health = StatusStore.Get(service.Name);
            GitHubBadgeStatus? deployBadge = string.IsNullOrEmpty(service.RepoName) ? null : await GitHubBadgeService.GetStatusAsync(service.RepoName, "deploy.yml", "master", cancellationToken);

            // Combine the health and deploy badge data into a single object
            result.Add(new ServiceInformation
            {
                Name = service.Name,
                Health = health,
                DeployBadge = deployBadge
            });
        }

        // Return the combined status information as a JSON response
        return Ok(result);
    }
}