using System.Collections.Concurrent;
using Dashboard.Models;

namespace Dashboard.Services;

/// <summary>
/// Singleton in-memory store for the latest deploy badge status of each repo.
/// </summary>
public class BadgeStore
{
    /// <summary>
    /// The latest badge status for each repo, keyed by "owner/repo".
    /// </summary>
    private readonly ConcurrentDictionary<string, GitHubBadgeStatus> Badges = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Updates or inserts the badge status for a repo.
    /// </summary>
    /// <param name="repo">The repository in "owner/repo" form.</param>
    /// <param name="status">The latest badge status.</param>
    public void Set(string repo, GitHubBadgeStatus status)
    {
        Badges[repo] = status;
    }

    /// <summary>
    /// Returns a snapshot of every repo's badge status.
    /// </summary>
    public Dictionary<string, GitHubBadgeStatus> GetAll()
    {
        return new(Badges, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes the badge status of every repo that isn't in the given list.
    /// </summary>
    /// <param name="repos">The repos to keep.</param>
    public void RemoveExcept(IEnumerable<string> repos)
    {
        HashSet<string> keep = new(repos, StringComparer.OrdinalIgnoreCase);
        foreach (string repo in Badges.Keys.Where(repo => !keep.Contains(repo))) Badges.TryRemove(repo, out _);
    }
}