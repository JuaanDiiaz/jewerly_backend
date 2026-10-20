using System;
using System.Threading;
using System.Threading.Tasks;
using J_A_Jewelry.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace J_A_Jewelry.Services;

/// <summary>
/// Daily 03:30-local sweep of the LogEvents table: Debug/Information/Warning
/// rows older than LogRetention:WarnDays (default 30) and Error/Fatal rows
/// older than LogRetention:ErrorDays (default 90) are deleted in bounded
/// batches so the MySQL undo/lock footprint stays small. Never crashes the
/// host — a failed sweep is a logged Warning and tomorrow it retries.
/// </summary>
public class LogRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LogRetentionOptions _options;
    private static readonly TimeSpan DelayBetweenBatches = TimeSpan.FromSeconds(5);

    public LogRetentionService(IServiceScopeFactory scopeFactory, IOptions<LogRetentionOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give migrations/startup a quiet minute, then sweep once, then
        // schedule the next run for tomorrow 03:30.
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSweepAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                Log.Warning("Log retention sweep failed: {Message}", exception.Message);
            }

            await Task.Delay(DelayUntil0330(), stoppingToken);
        }
    }

    /// <summary>Swept in batches of 5000 until a batch returns nothing; each
    /// batch is its own statement so MySQL row locks stay small.</summary>
    private async Task RunSweepAsync(CancellationToken stoppingToken)
    {
        var warnCutoff = DateTime.UtcNow.AddDays(-_options.WarnDays);
        var errorCutoff = DateTime.UtcNow.AddDays(-_options.ErrorDays);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IsetechcJewelryInventoryContext>();

        var deleted = 0;
        const int BatchSize = 5000;
        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await context.Set<LogEvent>().AsNoTracking()
                .Where(e =>
                    (e.Level != "Error" && e.Level != "Fatal" && e.TimeStamp < warnCutoff) ||
                    ((e.Level == "Error" || e.Level == "Fatal") && e.TimeStamp < errorCutoff))
                .OrderBy(e => e.Id)
                .Take(BatchSize)
                .Select(e => e.Id)
                .ToListAsync(stoppingToken);
            if (batch.Count == 0) break;

            deleted += await context.Set<LogEvent>()
                .Where(e => batch.Contains(e.Id))
                .ExecuteDeleteAsync(stoppingToken);
        }

        if (deleted > 0)
            Log.Information("Log retention sweep deleted {Deleted} LogEvents (warn>{WarnDays}d, error>{ErrorDays}d)",
                deleted, _options.WarnDays, _options.ErrorDays);
    }

    private static TimeSpan DelayUntil0330()
    {
        var now = DateTime.Now;
        var next = now.Date.AddHours(3).AddMinutes(30);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}

public class LogRetentionOptions
{
    public int WarnDays { get; set; } = 30;
    public int ErrorDays { get; set; } = 90;
}