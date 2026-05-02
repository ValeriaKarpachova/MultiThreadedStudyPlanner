using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace WpfApp2.Services
{
    public enum LogLevel { Debug, Info, Warning, Error, Fatal }

    public class LogEntry
    {
        public DateTime  Timestamp { get; init; }
        public LogLevel  Level     { get; init; }
        public string    Category  { get; init; } = "";
        public string    Message   { get; init; } = "";
        public string?   Exception { get; init; }

        public string Formatted =>
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level,-7}] [{Category,-16}] {Message}" +
            (Exception != null ? $"\n  EXCEPTION: {Exception}" : "");
    }

    public static class AppLogger
    {
        public static LogLevel MinLevel        { get; set; } = LogLevel.Debug;
        public static int      MaxFileSizeKb   { get; set; } = 512;   
        public static int      MaxBufferItems  { get; set; } = 500;  

        private static readonly string LogDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        private static string CurrentLogFile =>
            Path.Combine(LogDir, $"app_{DateTime.Today:yyyyMMdd}.log");

        private static readonly List<LogEntry> _buffer = new();
        public  static IReadOnlyList<LogEntry> Buffer  => _buffer.AsReadOnly();

        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public static event Action<LogEntry>? EntryAdded;

        public static void Log(LogLevel level, string category, string message,
                               Exception? ex = null)
        {
            if (level < MinLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level     = level,
                Category  = category,
                Message   = message,
                Exception = ex?.ToString()
            };

            lock (_buffer)
            {
                _buffer.Add(entry);
                if (_buffer.Count > MaxBufferItems)
                    _buffer.RemoveAt(0);
            }

            _ = WriteToFileAsync(entry);

            EntryAdded?.Invoke(entry);

            if (level >= LogLevel.Warning)
                System.Diagnostics.Debug.WriteLine(entry.Formatted);
        }

        public static void Debug  (string cat, string msg)              => Log(LogLevel.Debug,   cat, msg);
        public static void Info   (string cat, string msg)              => Log(LogLevel.Info,    cat, msg);
        public static void Warning(string cat, string msg)              => Log(LogLevel.Warning, cat, msg);
        public static void Error  (string cat, string msg, Exception? ex = null)
                                                                         => Log(LogLevel.Error,   cat, msg, ex);
        public static void Fatal  (string cat, string msg, Exception? ex = null)
                                                                         => Log(LogLevel.Fatal,   cat, msg, ex);

        public static List<LogEntry> GetBuffered(LogLevel minLevel = LogLevel.Debug,
                                                  string? category  = null)
        {
            lock (_buffer)
            {
                return _buffer.FindAll(e =>
                    e.Level >= minLevel &&
                    (category == null || e.Category == category));
            }
        }

        public static string ReadLogFile(DateTime? date = null)
        {
            var path = date.HasValue
                ? Path.Combine(LogDir, $"app_{date.Value:yyyyMMdd}.log")
                : CurrentLogFile;

            return File.Exists(path)
                ? File.ReadAllText(path, Encoding.UTF8)
                : "(файл логу не знайдено)";
        }

        public static IEnumerable<string> GetLogFiles()
        {
            if (!Directory.Exists(LogDir)) return Array.Empty<string>();
            return Directory.GetFiles(LogDir, "app_*.log");
        }

        public static void ClearBuffer()
        {
            lock (_buffer) { _buffer.Clear(); }
        }

        private static async System.Threading.Tasks.Task WriteToFileAsync(LogEntry entry)
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);

                RotateIfNeeded();

                await File.AppendAllTextAsync(
                    CurrentLogFile,
                    entry.Formatted + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
            finally { _fileLock.Release(); }
        }

        private static void RotateIfNeeded()
        {
            var path = CurrentLogFile;
            if (!File.Exists(path)) return;

            var info = new FileInfo(path);
            if (info.Length < MaxFileSizeKb * 1024) return;

            var archived = Path.Combine(LogDir,
                $"app_{DateTime.Today:yyyyMMdd}_{DateTime.Now:HHmmss}_archived.log");
            File.Move(path, archived);
        }
    }
}
