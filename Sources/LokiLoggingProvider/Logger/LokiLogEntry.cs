namespace LoggingProvider.Loki.Logger;

using System;

internal readonly record struct LokiLogEntry(DateTimeOffset Timestamp, LabelValues Labels, string Message);
