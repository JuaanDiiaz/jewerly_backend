using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using J_A_Jewelry.Models;
using J_A_Jewelry.Services.Identity;

namespace J_A_Jewelry.Controllers;

// Viewing logs is an admin area (they leak paths/errors); the client ingest
// stays open — splash/login-window errors must ship before a session exists.
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = Permissions.AdminData)]
public class LogsController : ControllerBase
{
    private const int MaxBatchEvents = 100;
    private const int MaxMessageLength = 4000;
    private const int MaxEventMetaLength = 8000;
    private const int MaxBatchesPerMinute = 30;

    private readonly IsetechcJewelryInventoryContext _context;
    private readonly IMemoryCache _rateLimiter;

    public LogsController(IsetechcJewelryInventoryContext context, IMemoryCache rateLimiter)
    {
        _context = context;
        _rateLimiter = rateLimiter;
    }

    // GET: api/Logs?level=Error,Warning&source=client&search=text&from=2026-10-01T00:00:00Z&to=...&page=1&page_size=50
    [HttpGet]
    public async Task<ActionResult<object>> GetLogs(
        [FromQuery] string? level = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Min(Math.Max(1, pageSize), 200);

        var query = _context.Set<LogEvent>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
        {
            // Accept both Serilog names and the client's lowercase set.
            var normalized = level.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => NormalizeLevel(l))
                .Where(l => l != null)
                .ToHashSet();
            if (normalized.Count > 0)
                query = query.Where(e => normalized.Contains(e.Level));
            else
                return Ok(new { page, pageSize, totalCount = 0, data = Array.Empty<LogEventDto>() });
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            var s = source.Trim().ToLowerInvariant();
            if (s is "server" or "client")
                query = query.Where(e => e.Source == s);
            else
                return Ok(new { page, pageSize, totalCount = 0, data = Array.Empty<LogEventDto>() });
        }

        if (from.HasValue) query = query.Where(e => e.TimeStamp >= from.Value);
        if (to.HasValue) query = query.Where(e => e.TimeStamp <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE over the message only — exception/properties stay searchable later
            // if a need shows up; unbounded LIKE over LONGTEXT is the slow path.
            var term = search.Trim().Substring(0, Math.Min(search.Trim().Length, 200));
            var pattern = $"%{EscapeLike(term)}%";
            query = query.Where(e => EF.Functions.Like(e.Message, pattern));
        }

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderByDescending(e => e.TimeStamp)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new LogEventDto
            {
                Id = e.Id,
                TimeStamp = e.TimeStamp,
                Level = e.Level,
                Source = e.Source,
                TraceId = e.TraceId,
                Message = e.Message,
                Exception = e.Exception,
                Properties = e.Properties
            })
            .ToListAsync();

        return Ok(new { page, pageSize, totalCount, data = rows });
    }

    // POST: api/Logs/client — batched ingest of client-side (Flutter app) log
    // events. Deliberately unauthenticated: splash/login-window errors ship
    // before any session exists; guards are size and rate, not identity.
    [AllowAnonymous]
    [HttpPost("client")]
    public async Task<ActionResult> PostClientLogs([FromBody] ClientLogBatchDto batch)
    {
        if (IsRateLimited())
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Too many log batches; try again later." });

        if (batch?.Events == null || batch.Events.Count == 0) return BadRequest("events must be a non-empty array");
        if (batch.Events.Count > MaxBatchEvents) return BadRequest($"Batch too large: max {MaxBatchEvents} events");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();
        var traceId = Request.Headers["X-TraceId"].ToString();
        var now = DateTime.UtcNow;

        var rows = new List<LogEvent>(batch.Events.Count);
        foreach (var e in batch.Events)
        {
            if (string.IsNullOrWhiteSpace(e.Message)) return BadRequest("each event needs a message");

            var level = MapClientLevel(e.Level);
            if (level == null) return BadRequest($"Unsupported level '{e.Level}' (expected debug|info|warning|error)");

            rows.Add(new LogEvent
            {
                TimeStamp = e.OccurredAt ?? now,
                Level = level,
                Source = "client",
                TraceId = e.TraceId ?? (traceId.Length > 0 ? traceId : null),
                Message = Truncate(e.Message, MaxMessageLength),
                Exception = e.Exception,
                // Meta (Context + Device + AppBaseUrl + device-side User-Agent) is
                // stored in the same JSON column: flat single blob per event.
                Properties = JsonSerializer.Serialize(new Dictionary<string, string?>
                {
                    ["context"] = Truncate(e.Context, MaxEventMetaLength),
                    ["device"] = Truncate(e.Device, MaxEventMetaLength),
                    ["appBaseUrl"] = e.AppBaseUrl,
                    ["userAgent"] = userAgent.Length > 0 ? userAgent : null,
                    ["ip"] = ip,
                })
            });
        }

        _context.Set<LogEvent>().AddRange(rows);
        await _context.SaveChangesAsync();

        return Ok(new { accepted = rows.Count });
    }

    /// <summary>True when this request exceeds the per-IP batch quota.
    /// (Named for what a `true` means; the caller 429s on it.)</summary>
    private bool IsRateLimited()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"client-logs:{ip}";
        var now = DateTime.UtcNow;

        if (_rateLimiter.TryGetValue(key, out ClientLogWindow? window) && window != null)
        {
            if (window.Start > now.AddMinutes(-1))
            {
                if (window.Count >= MaxBatchesPerMinute) return true;
                window.Count++;
                return false;
            }
            window.Start = now;
            window.Count = 1;
            return false;
        }

        _rateLimiter.Set(key, new ClientLogWindow(now, 1), TimeSpan.FromMinutes(2));
        return false;
    }

    private static string? MapClientLevel(string? level) => (level ?? "").Trim().ToLowerInvariant() switch
    {
        "debug" => "Debug",
        "info" => "Information",
        "information" => "Information",
        "warning" => "Warning",
        "error" => "Error",
        _ => null
    };

    private static readonly string[] AcceptedServerLevels = { "Debug", "Information", "Warning", "Error", "Fatal" };

    private static string? NormalizeLevel(string level)
    {
        var l = level.ToLowerInvariant();
        return l switch
        {
            "debug" => "Debug",
            "info" => "Information",
            "information" => "Information",
            "warning" => "Warning",
            "error" => "Error",
            "fatal" => "Fatal",
            _ => AcceptedServerLevels.Contains(level) ? level : null
        };
    }

    /// <summary>%-escaping for the term inside the caller's wildcard pattern.</summary>
    private static string EscapeLike(string term) =>
        term.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    private static string Truncate(string? value, int max) =>
        (value ?? "").Length <= max ? value ?? "" : (value ?? "").Substring(0, max);

    private sealed class ClientLogWindow(DateTime start, int count)
    {
        public DateTime Start { get; set; } = start;
        public int Count { get; set; } = count;
    }
}