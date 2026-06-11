using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSoulsModLoader.Services;

public class LogService
{
    private readonly List<LogEntry> _logs = new();
    private readonly int _maxLogs = 1000;

    public event EventHandler<LogEntry>? LogAdded;

    public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            Level = level
        };

        _logs.Add(entry);
        if (_logs.Count > _maxLogs)
            _logs.RemoveAt(0);

        LogAdded?.Invoke(this, entry);
    }

    public void Clear()
    {
        _logs.Clear();
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = "";
    public LogLevel Level { get; set; }

    public override string ToString()
    {
        var timeStr = Timestamp.ToString("HH:mm:ss.fff");
        var levelStr = Level.ToString().ToUpper();
        return $"[{timeStr}] [{levelStr}] {Message}";
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
