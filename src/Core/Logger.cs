using System;
using System.Text;

namespace AutoUSBToolkit.Core
{
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class LogMessageEventArgs : EventArgs
    {
        public DateTime Timestamp { get; private set; }
        public LogLevel Level { get; private set; }
        public string Message { get; private set; }

        public LogMessageEventArgs(LogLevel level, string message)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
        }

        public string FormattedText
        {
            get
            {
                string tag = "INFO";
                switch (Level)
                {
                    case LogLevel.Success: tag = "SUCCESS"; break;
                    case LogLevel.Warning: tag = "WARN"; break;
                    case LogLevel.Error: tag = "ERROR"; break;
                }
                return string.Format("[{0:HH:mm:ss}] [{1}] {2}", Timestamp, tag, Message);
            }
        }
    }

    public static class Logger
    {
        public static event EventHandler<LogMessageEventArgs> MessageLogged;
        private static readonly object _lock = new object();
        private static readonly StringBuilder _fullLog = new StringBuilder();

        public static void Log(LogLevel level, string message)
        {
            var args = new LogMessageEventArgs(level, message);
            lock (_lock)
            {
                _fullLog.AppendLine(args.FormattedText);
            }

            var handler = MessageLogged;
            if (handler != null)
            {
                handler(null, args);
            }
        } 

        public static void Info(string message) { Log(LogLevel.Info, message); }
        public static void Success(string message) { Log(LogLevel.Success, message); }
        public static void Warn(string message) { Log(LogLevel.Warning, message); }
        public static void Error(string message) { Log(LogLevel.Error, message); }

        public static string GetFullLog()
        {
            lock (_lock)
            {
                return _fullLog.ToString();
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _fullLog.Clear();
            }
        }
    }
}

