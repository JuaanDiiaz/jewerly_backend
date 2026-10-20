using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace J_A_Jewelry.Models;

/// <summary>
/// Read-only mapping of the diagnostics LogEvents table. NOT a domain table
/// and NOT part of any migration: the table is created/maintained by the
/// MySQL log sink (scripts/create_logevents_table.sql is the prod DDL
/// record), so the mapping is excluded from migrations in
/// IsetechcJewelryInventoryContext.ModelExtensions.OnModelCreatingPartial and
/// is queried via context.Set&lt;LogEvent&gt;() without a DbSet declaration.
/// Column names must stay in step with MySqlLogEventSink.cs.
/// </summary>
[Table("LogEvents")]
public class LogEvent
{
    public long Id { get; set; }

    /// <summary>UTC.</summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>Debug|Information|Warning|Error|Fatal.</summary>
    public string Level { get; set; } = "";

    /// <summary>'server' (written by the sink) or 'client' (ingest endpoint).</summary>
    public string Source { get; set; } = "";

    /// <summary>W3C trace id shared between the API and the app for one issue.</summary>
    public string? TraceId { get; set; }

    public string Message { get; set; } = "";
    public string? Exception { get; set; }

    /// <summary>JSON object with the remaining Serilog properties (ingest
    /// contexts, device blocks, structured log params).</summary>
    public string? Properties { get; set; }
}