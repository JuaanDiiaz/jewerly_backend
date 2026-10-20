using System;

namespace J_A_Jewelry.Models;

// DTOs for the Logs endpoints (pattern matches ReportDtos.cs): flat records,
// no navigation properties, so the ReferenceHandler.Preserve serializer never
// produces cycles — $values wrapping is unwrapped client-side.

/// <summary>GET api/Logs response row (server + client events).</summary>
public class LogEventDto
{
    public long Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Level { get; set; } = "";
    public string Source { get; set; } = "";
    public string? TraceId { get; set; }
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
    public string? Properties { get; set; }
}

/// <summary>POST api/Logs/client request event.</summary>
public class ClientLogEventDto
{
    /// <summary>debug|info|warning|error (mapped to Information/Warning/Error).</summary>
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    /// <summary>Free-form caller context, stored as a JSON sub-object.</summary>
    public string? Context { get; set; }
    /// <summary>Exception text when the client had one (formatted toString).</summary>
    public string? Exception { get; set; }
    public string? TraceId { get; set; }
    public string? Device { get; set; }
    /// <summary>UTC time of the event; falls back to now when absent.</summary>
    public DateTime? OccurredAt { get; set; }
    /// <summary>Base URL the app was pointed at — identifies which backend a client was talking to.</summary>
    public string? AppBaseUrl { get; set; }
}

/// <summary>POST api/Logs/client request body.</summary>
public class ClientLogBatchDto
{
    public List<ClientLogEventDto> Events { get; set; } = [];
}