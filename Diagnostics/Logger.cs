using System;
using System.IO;
using System.Text;

namespace MenubarDock.Diagnostics
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string _logDir;
        private static string _currentLogFile;

        static Logger()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logDir = Path.Combine(localAppData, "MenubarDock", "Logs");
                Directory.CreateDirectory(_logDir);
                _currentLogFile = Path.Combine(_logDir, $"menubar_{DateTime.UtcNow:yyyyMMdd}.log");
            }
            catch
            {
                _logDir = AppDomain.CurrentDomain.BaseDirectory;
                _currentLogFile = Path.Combine(_logDir, "menubar.log");
            }
        }

        public static string LogDirectory => _logDir;

        public static void Info(string message) => Log("INFO", message);
        public static void Warn(string message, Exception? ex = null) => Log("WARN", message, ex);
        public static void Error(string message, Exception? ex = null) => Log("ERROR", message, ex);

        private static void Log(string level, string message, Exception? ex = null)
        {
            try
            {
                lock (_lock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var sb = new StringBuilder();
                    sb.Append($"[{timestamp}] [{level.PadRight(5)}] {message}");
                    if (ex != null)
                    {
                        sb.AppendLine();
                        sb.Append($"Exception: {ex.Message}\n{ex.StackTrace}");
                    }
                    sb.AppendLine();
                    File.AppendAllText(_currentLogFile, sb.ToString(), Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
