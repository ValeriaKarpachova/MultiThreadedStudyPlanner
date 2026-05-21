using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WpfApp2.Services
{
    public class ChangeLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action     { get; set; } = "";
        public int     TaskId    { get; set; }
        public string  TaskName  { get; set; } = "";
        public string  Detail    { get; set; } = "";

        public override string ToString() =>
            $"{Timestamp:yyyy-MM-dd HH:mm:ss} | {Action,-6} | #{TaskId} \"{TaskName}\" | {Detail}";
    }

    public static class DataStorageService
    {
        private static readonly string DataDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        private static readonly string DataFile    = Path.Combine(DataDir, "tasks.spd");
        private static readonly string CacheFile   = Path.Combine(DataDir, "tasks.cache");
        private static readonly string JournalFile = Path.Combine(DataDir, "changelog.log");
        private static readonly string SubjectsFile = Path.Combine(DataDir, "subjects.spd");

        private static readonly byte[] Magic = { (byte)'S', (byte)'P', (byte)'D', 0x03 };

        private static List<TaskItem>? _cache;
        private static DateTime        _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(5);
        private static readonly ReaderWriterLockSlim _lock = new();

        private static readonly List<ChangeLogEntry> _journal = new();
        public static IReadOnlyList<ChangeLogEntry> Journal => _journal.AsReadOnly();

        private static readonly object _journalFileLock = new object();

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(DataDir))
                Directory.CreateDirectory(DataDir);
        }

        public static void Save(IEnumerable<TaskItem> tasks)
        {
            EnsureDirectory();
            var list = tasks.ToList();

            _lock.EnterWriteLock();
            try
            {
                WriteSpd(list, DataFile);
                WriteCacheManifest(list);
                _cache     = list;
                _cacheTime = DateTime.Now;
            }
            finally { _lock.ExitWriteLock(); }

            AppLogger.Log(LogLevel.Info, "DataStorage",
                $"Збережено {list.Count} задач у {DataFile}");
        }

        public static List<TaskItem> Load()
        {
            EnsureDirectory();

            _lock.EnterReadLock();
            try
            {
                if (_cache != null && DateTime.Now - _cacheTime < CacheTTL)
                {
                    AppLogger.Log(LogLevel.Debug, "DataStorage", "Дані з гарячого кешу");
                    return _cache;
                }
            }
            finally { _lock.ExitReadLock(); }

            if (File.Exists(CacheFile) && File.Exists(DataFile))
            {
                var cacheDate = File.GetLastWriteTime(CacheFile);
                var dataDate  = File.GetLastWriteTime(DataFile);
                if (cacheDate >= dataDate)
                {
                    var cached = ReadCacheManifest();
                    if (cached != null)
                    {
                        _lock.EnterWriteLock();
                        try { _cache = cached; _cacheTime = DateTime.Now; }
                        finally { _lock.ExitWriteLock(); }
                        AppLogger.Log(LogLevel.Debug, "DataStorage", "Дані з файлового кешу");
                        return cached;
                    }
                }
            }

            if (!File.Exists(DataFile))
            {
                AppLogger.Log(LogLevel.Warning, "DataStorage", "Файл даних не знайдено, повертаємо порожній список");
                return new List<TaskItem>();
            }

            var result = ReadSpd(DataFile);
            _lock.EnterWriteLock();
            try { _cache = result; _cacheTime = DateTime.Now; }
            finally { _lock.ExitWriteLock(); }

            AppLogger.Log(LogLevel.Info, "DataStorage", $"Завантажено {result.Count} задач з файлу");
            return result;
        }

        public static void SaveSubjects(IEnumerable<Subject> subjects)
        {
            EnsureDirectory();
            var list = subjects.ToList();
            using var fs = new FileStream(SubjectsFile, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs, Encoding.UTF8);
            bw.Write(list.Count);
            foreach (var s in list)
            {
                bw.Write(s.Id);
                WriteString(bw, s.Name);
                WriteString(bw, s.Color);
            }
        }

        public static List<Subject> LoadSubjects()
        {
            if (!File.Exists(SubjectsFile)) return new();
            using var fs = new FileStream(SubjectsFile, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs, Encoding.UTF8);
            int count = br.ReadInt32();
            var list = new List<Subject>(count);
            for (int i = 0; i < count; i++)
                list.Add(new Subject
                {
                    Id = br.ReadInt32(),
                    Name = ReadString(br),
                    Color = ReadString(br)
                });
            return list;
        }

        public static void InvalidateCache()
        {
            _lock.EnterWriteLock();
            try { _cache = null; _cacheTime = DateTime.MinValue; }
            finally { _lock.ExitWriteLock(); }
        }

        public static void LogChange(string action, TaskItem task, string detail = "")
        {
            var entry = new ChangeLogEntry
            {
                Timestamp = DateTime.Now,
                Action    = action,
                TaskId    = task.Id,
                TaskName  = task.Name ?? "",
                Detail    = detail
            };

            lock (_journal) { _journal.Add(entry); }

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    EnsureDirectory();
                    lock (_journalFileLock)
                    {
                        File.AppendAllText(
                            JournalFile,
                            entry.ToString() + Environment.NewLine,
                            Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log(LogLevel.Warning, "ChangeLog",
                        $"Не вдалося записати в журнал: {ex.Message}");
                }
            });

            AppLogger.Log(LogLevel.Debug, "ChangeLog", entry.ToString());
        }

        public static List<ChangeLogEntry> LoadJournalFromFile()
        {
            if (!File.Exists(JournalFile)) return new();
            var lines  = File.ReadAllLines(JournalFile, Encoding.UTF8);
            var result = new List<ChangeLogEntry>();
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 4) continue;
                if (!DateTime.TryParse(parts[0].Trim(), out var ts)) continue;
                result.Add(new ChangeLogEntry
                {
                    Timestamp = ts,
                    Action    = parts[1].Trim(),
                    TaskId    = parts.Length > 2 && int.TryParse(
                        parts[2].Trim().TrimStart('#').Split(' ')[0], out var id) ? id : 0,
                    TaskName  = parts.Length > 2
                        ? parts[2].Trim().Replace($"#{result.Count}", "").Trim() : "",
                    Detail    = parts.Length > 3 ? parts[3].Trim() : ""
                });
            }
            return result;
        }

        private static void WriteSpd(List<TaskItem> tasks, string path)
        {
            var flat = tasks
                .SelectMany(t => new[] { t }.Concat(t.SubTasks))
                .ToList();

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs, Encoding.UTF8);

            bw.Write(Magic);
            bw.Write(flat.Count);

            foreach (var t in flat)
            {
                bw.Write(t.Id);
                bw.Write(t.ParentId ?? -1);
                bw.Write(t.Priority);
                bw.Write(t.EstimatedHours);
                bw.Write(t.IsChecked);
                bw.Write(t.Deadline.HasValue ? t.Deadline.Value.Ticks : -1L);
                bw.Write(t.DeadlineTime.HasValue ? t.DeadlineTime.Value.Ticks : -1L);
                WriteString(bw, t.Name        ?? "");
                WriteString(bw, t.Description ?? "");
                WriteString(bw, t.TaskType    ?? "");
                bw.Write(t.SubjectId ?? -1);
            }
        }

        private static List<TaskItem> ReadSpd(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs, Encoding.UTF8);

            var magic = br.ReadBytes(4);
            bool isV2 = magic[3] == 0x02;
            bool isV3 = magic[3] == 0x03;
            if (magic[0] != 'S' || magic[1] != 'P' || magic[2] != 'D' ||
                (magic[3] != 0x01 && magic[3] != 0x02 && magic[3] != 0x03))
                throw new InvalidDataException("Невірний формат файлу SPD");

            int count = br.ReadInt32();
            var flat  = new List<TaskItem>(count);

            for (int i = 0; i < count; i++)
            {
                var t = new TaskItem
                {
                    Id             = br.ReadInt32(),
                    Priority       = 0,
                    EstimatedHours = 0
                };
                int parentRaw    = br.ReadInt32();
                t.ParentId       = parentRaw == -1 ? null : parentRaw;
                t.Priority       = br.ReadInt32();
                t.EstimatedHours = br.ReadDouble();
                t.IsChecked      = br.ReadBoolean();

                long dateTicks   = br.ReadInt64();
                t.Deadline       = dateTicks == -1 ? null : new DateTime(dateTicks);

                if (isV2 || isV3)
                {
                    long timeTicks = br.ReadInt64();
                    t.DeadlineTime = timeTicks == -1 ? null : new TimeSpan(timeTicks);
                }

                t.Name        = ReadString(br);
                t.Description = ReadString(br);
                t.TaskType    = ReadString(br);

                if (isV3)
                {
                    int subjectRaw = br.ReadInt32();
                    t.SubjectId = subjectRaw == -1 ? null : subjectRaw;
                }

                flat.Add(t);
            }

            var roots    = flat.Where(t => t.ParentId == null).ToList();
            var children = flat.Where(t => t.ParentId != null).ToList();
            foreach (var child in children)
            {
                var parent = roots.FirstOrDefault(r => r.Id == child.ParentId);
                parent?.SubTasks.Add(child);
            }
            return roots;
        }

        private static void WriteString(BinaryWriter bw, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }

        private static string ReadString(BinaryReader br)
        {
            int len   = br.ReadInt32();
            var bytes = br.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void WriteCacheManifest(List<TaskItem> tasks)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# cache generated {DateTime.Now:O}");
            sb.AppendLine($"# count={tasks.Count}");
            foreach (var t in tasks)
                sb.AppendLine($"{t.Id},{t.Name?.Replace(",", "\\,")}," +
                              $"{t.Deadline:yyyy-MM-dd},{t.DeadlineTimeString},{t.IsChecked}");
            File.WriteAllText(CacheFile, sb.ToString(), Encoding.UTF8);
        }

        private static List<TaskItem>? ReadCacheManifest() => null;
    }
}
