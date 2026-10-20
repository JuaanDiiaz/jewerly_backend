-- DDL record for the diagnostics log table. NOT executed by a migration:
-- the dev database gets it automatically via MySqlLogEventSink
-- auto-create on first write; production must get it run MANUALLY (same
-- discipline as every other prod change — see DATABASE.md; never
-- `dotnet ef database update`).
--
-- Kept in step with:
--   Serilog/MySqlLogEventSink.cs (writer, INSERT column order)
--   Models/LogEvent.cs           (read mapping, excluded from migrations)
--   Models/LogDtos.cs            (viewer/ingest DTOs)
-- Column contract: Id auto-increment; TimeStamp UTC datetime(3); Level one
-- of Debug|Information|Warning|Error|Fatal; Source 'server'|'client';
-- TraceId W3C trace id (32 hex); Properties a JSON object or NULL.

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;