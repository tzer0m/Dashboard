using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.Uptime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Dashboard.Services;

/// <summary>
/// Computes rolling 30-day uptime percentages and daily outage/gap history for services, based on the raw <see cref="UptimeRecord"/> check history.
/// </summary>
/// <param name="dbContextFactory">Factory used to create short-lived database contexts.</param>
/// <param name="configuration">The app configuration, used to read the health check interval.</param>
public sealed class UptimeService(IDbContextFactory<DashboardDbContext> dbContextFactory, IConfiguration configuration)
{
    /// <summary>
    /// The name of the Dashboard's own service entry. A gap in its own recorded history means Dashboard itself wasn't running, so it counts as downtime rather than unknown — nothing else could have been recording it either way.
    /// </summary>
    private const string DashboardServiceName = "Dashboard";

    /// <summary>
    /// Number of days of history included in the uptime window.
    /// </summary>
    private const int WindowDays = 30;

    /// <summary>
    /// Multiplier applied to the check interval beyond which a gap between two consecutive checks is treated as missing data rather than a real, continuously-observed state.
    /// </summary>
    private const double GapToleranceMultiplier = 2.5;

    /// <summary>
    /// Factory used to create a short-lived database context per query.
    /// </summary>
    private readonly IDbContextFactory<DashboardDbContext> DbContextFactory = dbContextFactory;

    /// <summary>
    /// The expected interval between consecutive checks for a single service.
    /// </summary>
    private readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(configuration.GetValue("HealthCheck:IntervalSeconds", 60));

    /// <summary>
    /// Computes the 30-day uptime percentage and daily bar history for a single service.
    /// </summary>
    /// <param name="serviceName">The name of the service to compute uptime for.</param>
    /// <param name="cancellationToken">Token that signals when the request is cancelled.</param>
    public async Task<UptimeSummary> GetUptimeSummaryAsync(string serviceName, CancellationToken cancellationToken)
    {
        // Set the window to the last 30 days, starting at midnight UTC 30 days ago.
        DateTime windowEnd = DateTime.UtcNow;
        DateTime windowStart = windowEnd.AddDays(-WindowDays);
        TimeSpan gapThreshold = CheckInterval * GapToleranceMultiplier;

        // Query the raw uptime records for this service in the window, ordered by timestamp.
        await using DashboardDbContext dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        List<UptimeRecord> records = await dbContext.UptimeRecords.Where(r => r.ServiceName == serviceName && r.CheckedAtUtc >= windowStart && r.CheckedAtUtc <= windowEnd).OrderBy(r => r.CheckedAtUtc).ToListAsync(cancellationToken);
        List<(DateTime Start, DateTime End, UptimeDayStatus Status)> segments = BuildSegments(records, windowEnd, gapThreshold, serviceName == DashboardServiceName);

        // Compute the daily bars and uptime percentage from the segments.
        double? uptimePercent = CalculateUptimePercent(segments);
        List<DailyUptimeBar> days = BuildDailyBars(segments, windowStart, windowEnd);

        // Return the summary record.
        return new UptimeSummary(uptimePercent, days);
    }

    /// <summary>
    /// Walks the ordered check history and produces a sequence of contiguous time segments, each classified as up, down, or unknown (missing data).
    /// </summary>
    /// <param name="records">The ordered check history for a single service within the window.</param>
    /// <param name="windowEnd">The end of the 30-day window (now).</param>
    /// <param name="gapThreshold">The interval beyond which a gap between checks is treated as missing data.</param>
    /// <param name="treatGapsAsDown">Whether gaps should be classified as down rather than unknown, used for Dashboard's own history.</param>
    private static List<(DateTime Start, DateTime End, UptimeDayStatus Status)> BuildSegments(List<UptimeRecord> records, DateTime windowEnd, TimeSpan gapThreshold, bool treatGapsAsDown)
    {
        List<(DateTime Start, DateTime End, UptimeDayStatus Status)> segments = [];

        // If there's no data at all yet, report nothing rather than fabricating a verdict for the whole window.
        if (records.Count == 0)
        {
            return segments;
        }

        // Walk through the records and build segments. The time before the first record is intentionally excluded — it means monitoring hadn't started yet, not that the service was down.
        for (int i = 0; i < records.Count - 1; i++)
        {
            // Set the current segment's end to the next record's timestamp.
            DateTime start = records[i].CheckedAtUtc;
            DateTime end = records[i + 1].CheckedAtUtc;
            TimeSpan gap = end - start;

            // If the gap is larger than the threshold, insert a missing-data segment, otherwise extend the current segment to the next record.
            UptimeDayStatus status = gap > gapThreshold ? (treatGapsAsDown ? UptimeDayStatus.Down : UptimeDayStatus.Unknown) : (records[i].IsUp ? UptimeDayStatus.Up : UptimeDayStatus.Down);
            segments.Add((start, end, status));
        }

        // Add segment from last record to window end.
        UptimeRecord last = records[^1];
        TimeSpan trailingGap = windowEnd - last.CheckedAtUtc;
        UptimeDayStatus trailingStatus = trailingGap > gapThreshold ? (treatGapsAsDown ? UptimeDayStatus.Down : UptimeDayStatus.Unknown) : (last.IsUp ? UptimeDayStatus.Up : UptimeDayStatus.Down);
        segments.Add((last.CheckedAtUtc, windowEnd, trailingStatus));
        return segments;
    }

    /// <summary>
    /// Computes the overall uptime percentage across all segments, excluding any time classified as unknown from both the numerator and denominator.
    /// </summary>
    /// <param name="segments">The classified time segments for the window.</param>
    private static double? CalculateUptimePercent(List<(DateTime Start, DateTime End, UptimeDayStatus Status)> segments)
    {
        // Set the total up and down time, excluding unknown segments.
        TimeSpan knownDuration = TimeSpan.Zero;
        TimeSpan downDuration = TimeSpan.Zero;

        // Walk through the segments and accumulate the known and down durations.
        foreach ((DateTime start, DateTime end, UptimeDayStatus status) in segments)
        {
            //  Skip unknown segments.
            if (status == UptimeDayStatus.Unknown)
            {
                continue;
            }

            // Accumulate the known duration and, if down, the down duration.
            TimeSpan duration = end - start;
            knownDuration += duration;

            // If the segment is down, accumulate the down duration.
            if (status == UptimeDayStatus.Down)
            {
                downDuration += duration;
            }
        }

        // If there is no known duration, return null to indicate that the uptime percentage is undefined, otherwise compute the uptime percentage as (known - down) / known.
        return knownDuration == TimeSpan.Zero ? null : (1 - (downDuration / knownDuration)) * 100;
    }

    /// <summary>
    /// Buckets the segments into calendar days and assigns each day a worst-case status: any downtime makes the day red, otherwise any gap makes it orange,  otherwise it's green.
    /// </summary>
    /// <param name="segments">The classified time segments for the window.</param>
    /// <param name="windowStart">The start of the 30-day window.</param>
    /// <param name="windowEnd">The end of the 30-day window (now).</param>
    private static List<DailyUptimeBar> BuildDailyBars(List<(DateTime Start, DateTime End, UptimeDayStatus Status)> segments, DateTime windowStart, DateTime windowEnd)
    {
        // Set the start of the first day to midnight UTC of the window start, and the end of the last day to midnight UTC of the window end.
        DateOnly firstDay = DateOnly.FromDateTime(windowStart);
        DateOnly lastDay = DateOnly.FromDateTime(windowEnd);

        // Initialize the list of daily bars.
        Dictionary<DateOnly, UptimeDayStatus> worstStatusByDay = [];

        // Walk through the segments and assign each segment to the corresponding day(s).
        foreach ((DateTime start, DateTime end, UptimeDayStatus status) in segments)
        {
            // Loop through each day that the segment spans and update the worst status for that day.
            DateTime cursor = start;
            while (cursor < end)
            {
                DateOnly day = DateOnly.FromDateTime(cursor);
                DateTime dayEnd = day.ToDateTime(TimeOnly.MinValue).AddDays(1);
                DateTime segmentPartEnd = end < dayEnd ? end : dayEnd;

                // Update the worst status for the day: Down > Unknown > Up.
                if (!worstStatusByDay.TryGetValue(day, out UptimeDayStatus existing) || status > existing)
                {
                    worstStatusByDay[day] = status;
                }

                // Move the cursor to the end of the current segment part.
                cursor = segmentPartEnd;
            }
        }

        // Build the list of daily bars from the worst status by day, filling in any missing days with Up status.
        List<DailyUptimeBar> days = [];
        for (DateOnly day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            days.Add(new DailyUptimeBar(day, worstStatusByDay.GetValueOrDefault(day, UptimeDayStatus.Up)));
        }
        return days;
    }
}