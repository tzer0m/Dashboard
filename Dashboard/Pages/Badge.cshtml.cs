using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dashboard.Pages;

/// <summary>
/// Returns a shields.io endpoint-compatible JSON payload reflecting the latest
/// GitHub Actions deploy status for the given repo, authenticated server-side
/// so private repos work without exposing a token to the client.
/// </summary>
public class BadgeModel(GitHubBadgeService gitHubBadgeService) : PageModel
{
    /// <summary>
    /// Returns the badge JSON for the given owner/repo.
    /// </summary>
    /// <param name="repo">The repository in "owner/repo" form.</param>
    public async Task<IActionResult> OnGetAsync(string repo)
    {
        if (string.IsNullOrEmpty(repo)) return BadRequest();

        GitHubBadgeStatus status = await gitHubBadgeService.GetStatusAsync(repo, "deploy.yml", "master");

        return new JsonResult(new
        {
            schemaVersion = 1,
            label = "deploy",
            message = status.Message,
            color = status.Color
        });
    }
}