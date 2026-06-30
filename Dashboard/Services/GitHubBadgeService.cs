using System.Text.Json;
using Dashboard.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Services;

/// <summary>
/// Queries the GitHub Actions API for the latest workflow run status on a given repo, authenticated so private repos are accessible. Results are cached briefly to avoid hammering GitHub's API on every badge render.
/// </summary>
public class GitHubBadgeService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache)
{
    /// <summary>
    /// Cache duration for GitHub badge results, in seconds. This is set to 60 seconds to avoid hitting GitHub's API rate limits.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Fetches the latest run status for the given owner/repo and workflow file, returning a shields.io-compatible badge message and color. Cached for 60 seconds per repo.
    /// </summary>
    /// <param name="ownerRepo">The repository in "owner/repo" form.</param>
    /// <param name="workflowFile">The workflow filename, e.g. "deploy.yml".</param>
    /// <param name="branch">The branch to check.</param>
    public async Task<GitHubBadgeStatus> GetStatusAsync(string ownerRepo, string workflowFile, string branch)
    {
        // Check if the status is already cached
        string cacheKey = $"badge:{ownerRepo}:{workflowFile}:{branch}";
        if (memoryCache.TryGetValue(cacheKey, out GitHubBadgeStatus? cached) && cached is not null)
            return cached;

        // Get the token and construct the API URL
        string token = configuration["GitHub:AccessToken"] ?? string.Empty;
        string url = $"https://api.github.com/repos/{ownerRepo}/actions/workflows/{workflowFile}/runs?branch={branch}&per_page=1";

        // Create the request with the necessary headers
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Add("User-Agent", "Dashboard");

        // Send the request and get the response
        HttpResponseMessage response = await httpClient.SendAsync(request);

        // If the response is not successful, return a default "unknown" status
        GitHubBadgeStatus result;
        if (!response.IsSuccessStatusCode)
        {
            result = new GitHubBadgeStatus { Message = "unknown", Color = "9e9e9e" };
        }
        else
        {
            // Parse the response JSON to get the latest run status
            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement.ArrayEnumerator runs = document.RootElement.GetProperty("workflow_runs").EnumerateArray();

            // If there are no runs, return "unknown", otherwise get the status of the latest run
            if (!runs.MoveNext())
            {
                result = new GitHubBadgeStatus { Message = "unknown", Color = "9e9e9e" };
            }
            else
            {
                JsonElement latestRun = runs.Current;
                string status = latestRun.GetProperty("status").GetString() ?? string.Empty;
                if (status != "completed")
                {
                    result = new GitHubBadgeStatus { Message = "running", Color = "dfb317" };
                }
                else
                {
                    string conclusion = latestRun.GetProperty("conclusion").GetString() ?? string.Empty;
                    result = conclusion == "success" ? new GitHubBadgeStatus { Message = "passing", Color = "198754" } : new GitHubBadgeStatus { Message = "failing", Color = "dc3545" };
                }
            }
        }

        // Cache the result for future requests and return
        memoryCache.Set(cacheKey, result, CacheDuration);
        return result;
    }
}