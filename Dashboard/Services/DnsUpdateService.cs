using Dashboard.Data;
using Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Services;

/// <summary>
/// Service for reading DNS update records from the database.
/// </summary>
/// <remarks>
/// Initialises a new instance of <see cref="DnsUpdateService"/>.
/// </remarks>
/// <param name="context">The database context.</param>
public class DnsUpdateService(DashboardDbContext context)
{
    private readonly DashboardDbContext Context = context;

    /// <summary>
    /// Returns the most recent DNS update records, latest first.
    /// </summary>
    /// <param name="count">The number of records to return.</param>
    /// <returns>A list of <see cref="DnsUpdate"/> records.</returns>
    public async Task<List<DnsUpdate>> GetRecentAsync(int count = 4)
    {
        return await Context.DnsUpdates.OrderByDescending(d => d.RecordedAt).Take(count).ToListAsync();
    }
}