using System;

namespace Translator.Model.Log
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }

        public LogLevel Level { get; set; }

        public string Message { get; set; }

        public string Module { get; set; }

        public LogEntry()
        { }

        public LogEntry(DateTime timestamp, string message, string module, LogLevel level)
        {
            Timestamp = timestamp;
            Message = message;
            Module = module;
            Level = level;
        }

        public override string ToString()
        {
            return $"[{Timestamp.ToShortDateString()} @ {Timestamp.ToShortTimeString()} ({Module})]: {Message}";
        }
    }
}
