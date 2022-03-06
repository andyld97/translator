using System;

namespace Translator.Model.Log
{
    public static class Logger
    {
        public delegate void onAddedLogEntry(LogEntry logEntry);
        public static event onAddedLogEntry OnAddedLogEntry;

        public static void LogDebug(string message, string module)
        {
            Log(LogLevel.Debug, message, module);
        }

        public static void LogInformation(string message, string module)
        {
            Log(LogLevel.Information, message, module);
        }

        public static void LogWarning(string message, string module)
        {
            Log(LogLevel.Warning, message, module);
        }

        public static void LogError(string message, string module)
        {
            Log(LogLevel.Error, message, module);
        }

        private static void Log(LogLevel level, string message, string module)
        {
            OnAddedLogEntry?.Invoke(new LogEntry() { Timestamp = DateTime.Now, Level = level, Message = message, Module = module });
        }
    }
}
