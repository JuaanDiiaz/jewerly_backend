using System.Text.Json;
using MySqlConnector;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace J_A_Jewelry.Serilog;

/// <summary>
/// Serilog sink writing batches of log events to a LogEvents table in MySQL
/// (dev + prod). Owns the schema — the same column set is mirrored in
/// scripts/create_logevents_table.sql and the read-only EF mapping of
/// Models/LogEvent.cs; change all three together.
/// This is a client of the server pipeline (Source is always 'server');
/// client-sourced events are written by LogsController's ingest endpoint.
/// Never throws into the logging pipeline — a dead database must not take
/// the API down, the events are simply lost from this sink.
/// </summary>
public class MySqlLogEventSink : IBatchedLogEventSink
{
    private const string InsertSql =
        """
        INSERT INTO LogEvents (TimeStamp, Level, Source, TraceId, Message, Exception, Properties)
        VALUES (?ts, ?level, 'server', ?traceId, ?message, ?exception, ?properties)
        """;

    private const string CreateTableSql =
        """
        CREATE TABLE IF NOT EXISTS LogEvents (
          Id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
          TimeStamp DATETIME(3) NOT NULL,
          Level VARCHAR(32) NOT NULL,
          Source VARCHAR(16) NOT NULL,
          TraceId VARCHAR(64) NULL,
          Message TEXT NOT NULL,
          Exception LONGTEXT NULL,
          Properties JSON NULL,
          PRIMARY KEY (Id),
          KEY IX_LogEvents_TimeStamp (TimeStamp),
          KEY IX_LogEvents_Level (Level),
          KEY IX_LogEvents_Source (Source),
          KEY IX_LogEvents_TraceId (TraceId)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
        """;

    private readonly string _connectionString;
    private bool _tableEnsured;

    public MySqlLogEventSink(string connectionString) => _connectionString = connectionString;

    public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
    {
        var batch = events as IReadOnlyCollection<LogEvent> ?? events.ToList();
        if (batch.Count == 0) return;

        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            if (!_tableEnsured)
            {
                await using var createCommand = new MySqlCommand(CreateTableSql, connection);
                await createCommand.ExecuteNonQueryAsync();
                _tableEnsured = true;
            }

            await using var insertCommand = new MySqlCommand(InsertSql, connection);
            var tsParameter = insertCommand.Parameters.Add("?ts", MySqlDbType.DateTime);
            var levelParameter = insertCommand.Parameters.Add("?level", MySqlDbType.VarChar);
            var traceIdParameter = insertCommand.Parameters.Add("?traceId", MySqlDbType.VarChar);
            var messageParameter = insertCommand.Parameters.Add("?message", MySqlDbType.Text);
            var exceptionParameter = insertCommand.Parameters.Add("?exception", MySqlDbType.LongText);
            var propertiesParameter = insertCommand.Parameters.Add("?properties", MySqlDbType.JSON);

            foreach (var logEvent in batch)
            {
                tsParameter.Value = logEvent.Timestamp.UtcDateTime;
                levelParameter.Value = MapLevel(logEvent.Level.ToString());
                traceIdParameter.Value = (object?)ReadTraceId(logEvent) ?? DBNull.Value;
                messageParameter.Value = logEvent.RenderMessage();
                exceptionParameter.Value = (object?)logEvent.Exception?.ToString() ?? DBNull.Value;
                propertiesParameter.Value = (object?)SerializeProperties(logEvent) ?? DBNull.Value;
                await insertCommand.ExecuteNonQueryAsync();
            }
        }
        catch
        {
            // Deliberately swallowed: logging must never break the API. If the
            // database is down, file + console sinks still carry these events.
        }
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    /// <summary>Serilog names use "Information"/"Fatal"; the table contract is
    /// the flat set Debug|Information|Warning|Error|Fatal. Stored as-is.</summary>
    public static string MapLevel(string serilogLevel) => serilogLevel;

    private static string? ReadTraceId(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("TraceId", out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;

    /// <summary>Remaining scalar properties as a JSON object (TraceId excluded —
    /// it has its own column). Sequences/objects render compactly.</summary>
    private static string? SerializeProperties(LogEvent logEvent)
    {
        var values = new Dictionary<string, object?>();
        foreach (var (name, value) in logEvent.Properties)
            if (name != "TraceId")
                values[name] = RenderValue(value);

        if (values.Count == 0) return null;
        return JsonSerializer.Serialize(values);
    }

    private static object? RenderValue(LogEventPropertyValue value) => value switch
    {
        ScalarValue scalar => scalar.Value,
        SequenceValue sequence => sequence.Elements.Select(RenderValue).ToArray(),
        StructureValue structure => structure.Properties.ToDictionary(
            p => p.Name, p => RenderValue(p.Value)),
        DictionaryValue dictionary => dictionary.Elements.ToDictionary(
            e => e.Key.Value?.ToString() ?? "", e => RenderValue(e.Value)),
        _ => value.ToString()
    };
}